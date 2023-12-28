using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Trade.Dtos;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson.IO;
using Nest;
using Nethereum.Util;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace AwakenServer.Trade
{
    public interface ITradePairMarketDataProvider
    {
        Task UpdateTotalSupplyAsync(string chainId, Guid tradePairId, DateTime timestamp, BigDecimal lpTokenAmount,
            string supply = null);

        Task UpdateTradeRecordAsync(string chainId, Guid tradePairId, DateTime timestamp, double volume,
            double tradeValue);

        Task UpdateLiquidityAsync(string chainId, Guid tradePairId, DateTime timestamp, double price, double priceUSD,
            double tvl, double valueLocked0, double valueLocked1);

        Task FlushTotalSupplyCacheToSnapshotAsync(string key);

        Task FlushTradeRecordCacheToSnapshotAsync(string key);

        Task<TradePairMarketDataSnapshot> GetLatestTradePairMarketDataAsync(string chainId, Guid tradePairId);

        Task<Index.TradePairMarketDataSnapshot> GetTradePairMarketDataIndexAsync(string chainId, Guid tradePairId,
            DateTime snapshotTime);

        DateTime GetSnapshotTime(DateTime time);

        Task<Index.TradePairMarketDataSnapshot>
            GetLatestTradePairMarketDataIndexAsync(string chainId, Guid tradePairId);

        Task<List<Index.TradePairMarketDataSnapshot>> GetIndexListAsync(string chainId, Guid tradePairId,
            DateTime? timestampMin = null, DateTime? timestampMax = null);
    }

    public class TradePairMarketDataProvider : ITransientDependency, ITradePairMarketDataProvider
    {
        private readonly INESTRepository<Index.TradePairMarketDataSnapshot, Guid> _snapshotIndexRepository;
        private readonly INESTRepository<Index.TradePair, Guid> _tradePairIndexRepository;
        private readonly ITradeRecordAppService _tradeRecordAppService;
        private readonly IDistributedEventBus _distributedEventBus;
        private readonly IObjectMapper _objectMapper;
        private readonly IBus _bus;
        private readonly IDistributedCache<UpdateTotalSupplyBatch> _totalSupplyAccumulationCache;
        private readonly IDistributedCache<UpdateTradeRecordBatch> _tradeRecordAccumulationCache;
        private readonly ILogger<TradePairMarketDataProvider> _logger;
        private readonly TradeRecordOptions _tradeRecordOptions;
        private readonly IAbpDistributedLock _distributedLock;

        private static DateTime lastWriteTime;

        private static BigDecimal lastTotal;
        // private readonly IDatabase _database;

        public TradePairMarketDataProvider(
            INESTRepository<Index.TradePairMarketDataSnapshot, Guid> snapshotIndexRepository,
            INESTRepository<Index.TradePair, Guid> tradePairIndexRepository,
            ITradeRecordAppService tradeRecordAppService,
            IDistributedEventBus distributedEventBus,
            IBus bus,
            IObjectMapper objectMapper,
            IAbpDistributedLock distributedLock,
            IDistributedCache<UpdateTotalSupplyBatch> totalSupplyAccumulationCache,
            IDistributedCache<UpdateTradeRecordBatch> tradeRecordAccumulationCache,
            ILogger<TradePairMarketDataProvider> logger,
            IDistributedCache<CacheKeys> cacheKeys,
            IDistributedCache<UpdateLiquidityBatch> updateLiquidityCache,
            IOptionsSnapshot<TradeRecordOptions> tradeRecordOptions)
        {
            _snapshotIndexRepository = snapshotIndexRepository;
            _tradePairIndexRepository = tradePairIndexRepository;
            _tradeRecordAppService = tradeRecordAppService;
            _distributedEventBus = distributedEventBus;
            _objectMapper = objectMapper;
            _bus = bus;
            _distributedLock = distributedLock;
            _totalSupplyAccumulationCache = totalSupplyAccumulationCache;
            _tradeRecordAccumulationCache = tradeRecordAccumulationCache;
            _logger = logger;
            _tradeRecordOptions = tradeRecordOptions.Value;
        }

        public async Task FlushTradeRecordCacheToSnapshotAsync(string cacheKey)
        {
            await using var handle = await _distributedLock.TryAcquireAsync(cacheKey);

            var value = await _tradeRecordAccumulationCache.GetAsync(cacheKey);
            if (value != null)
            {
                if (DateTime.UtcNow.Subtract(value.CreateTime).TotalSeconds >=
                    _tradeRecordOptions.BatchFlushTimePeriod ||
                    value.TradeCount >= _tradeRecordOptions.BatchFlushCount)
                {
                    _logger.LogInformation(
                        "FlushTradeRecordCacheToSnapshot start.cacheKey:{cacheKey},chanId:{chanId},tradePairId:{tradePairId},timestamp:{timestamp},volume:{volume},tradeValue:{tradeValue},tradeCount:{tradeCount}",
                        cacheKey, value.ChanId, value.TradePairId, value.Timestamp, value.Volume, value.TradeValue,
                        value.TradeCount);
                    await _updateTradeRecordAsync(value.ChanId, value.TradePairId, value.Timestamp, value.Volume,
                        value.TradeValue, value.TradeCount);
                    _tradeRecordAccumulationCache.Remove(cacheKey);
                }
            }
        }

        public async Task FlushTotalSupplyCacheToSnapshotAsync(string cacheKey)
        {
            await using var handle = await _distributedLock.TryAcquireAsync(cacheKey);

            var value = await _totalSupplyAccumulationCache.GetAsync(cacheKey);
            if (value == null)
            {
                return;
            }

            if (DateTime.UtcNow.Subtract(value.LastTime).TotalSeconds >=
                _tradeRecordOptions.BatchFlushTimePeriod)
            {
                _logger.LogInformation(
                    "FlushTotalSupplyCacheToSnapshot start.cacheKey:{cacheKey},chanId:{chanId},tradePairId:{tradePairId},timestamp:{timestamp},totalSupply:{totalSupply}",
                    cacheKey, value.ChanId, value.TradePairId, value.Timestamp, value.TotalSupply);
                await _updateTotalSupplyAsync(value.ChanId, value.TradePairId, value.Timestamp,
                    BigDecimal.Parse(value.TotalSupply));
                _totalSupplyAccumulationCache.Remove(cacheKey);
            }
        }

        public async Task UpdateTotalSupplyAsync(string chainId, Guid tradePairId, DateTime timestamp,
            BigDecimal lpTokenAmount, string supply = null)
        {
            var lockName = string.Format("{0}-{1}-{2}", chainId,
                tradePairId, GetSnapshotTime(timestamp));

            _logger.LogInformation(
                "UpdateTotalSupply,chainId:{chainId},tradePairId:{tradePairId},timestamp:{timestamp},lpTokenAmount:{lpTokenAmount},supply:{supply}",
                chainId, tradePairId, timestamp, lpTokenAmount, supply);
            await using var handle = await _distributedLock.TryAcquireAsync(lockName);

            var value = await _totalSupplyAccumulationCache.GetAsync(lockName);
            if (value == null)
            {
                _totalSupplyAccumulationCache.Set(lockName, new UpdateTotalSupplyBatch
                {
                    LastTime = DateTime.UtcNow,
                    TotalSupply = lpTokenAmount.ToString(),
                    ChanId = chainId,
                    TradePairId = tradePairId,
                    Timestamp = timestamp
                });
                return;
            }

            lpTokenAmount += BigDecimal.Parse(value.TotalSupply);
            var span = DateTime.UtcNow.Subtract(value.LastTime).TotalSeconds;

            if (span < _tradeRecordOptions.BatchFlushTimePeriod)
            {
                await _totalSupplyAccumulationCache.SetAsync(lockName, new UpdateTotalSupplyBatch
                {
                    LastTime = value.LastTime,
                    TotalSupply = lpTokenAmount.ToString()
                });
                return;
            }

            await _updateTotalSupplyAsync(chainId, tradePairId, timestamp, lpTokenAmount, supply);
            await _totalSupplyAccumulationCache.RemoveAsync(lockName);
        }

        private async Task _updateTotalSupplyAsync(string chainId, Guid tradePairId, DateTime timestamp,
            BigDecimal lpTokenAmount, string supply = null)
        {
            var snapshotTime = GetSnapshotTime(timestamp);
            var marketData = await GetTradePairMarketDataIndexAsync(chainId, tradePairId, snapshotTime);
            if (marketData == null)
            {
                var lastMarketData =
                    await GetLatestTradePairMarketDataIndexAsync(chainId, tradePairId, snapshotTime);
                var totalSupply = lpTokenAmount;
                if (lastMarketData != null)
                {
                    totalSupply += BigDecimal.Parse(lastMarketData.TotalSupply);
                }

                marketData = new Index.TradePairMarketDataSnapshot()
                {
                    Id = Guid.NewGuid(),
                    ChainId = chainId,
                    TradePairId = tradePairId,
                    TotalSupply = string.IsNullOrWhiteSpace(supply) ? totalSupply.ToNormalizeString() : supply,
                    Timestamp = snapshotTime
                };
                if (lastMarketData != null)
                {
                    marketData.Price = lastMarketData.Price;
                    marketData.PriceUSD = lastMarketData.PriceUSD;
                    marketData.TVL = lastMarketData.TVL;
                    marketData.ValueLocked0 = lastMarketData.ValueLocked0;
                    marketData.ValueLocked1 = lastMarketData.ValueLocked1;
                }


                marketData.TradeAddressCount24h =
                    await _tradeRecordAppService.GetUserTradeAddressCountAsync(chainId, tradePairId,
                        timestamp.AddDays(-1), timestamp);
                await _snapshotIndexRepository.AddAsync(marketData);
                await AddOrUpdateTradePairIndexAsync(marketData);
            }
            else
            {
                var totalSupply = BigDecimal.Parse(marketData.TotalSupply);
                marketData.TotalSupply =
                    string.IsNullOrWhiteSpace(supply) ? (totalSupply + lpTokenAmount).ToNormalizeString() : supply;

                await _snapshotIndexRepository.UpdateAsync(marketData);
                await AddOrUpdateTradePairIndexAsync(marketData);
            }

            //nie:The current snapshot is not up-to-date. The latest snapshot needs to update TotalSupply 
            var latestMarketData = await GetLatestTradePairMarketDataIndexAsync(chainId, tradePairId);
            if (latestMarketData != null && latestMarketData.Timestamp > snapshotTime)
            {
                latestMarketData.TotalSupply = string.IsNullOrWhiteSpace(supply)
                    ? (BigDecimal.Parse(latestMarketData.TotalSupply) + lpTokenAmount).ToNormalizeString()
                    : supply;
                await _snapshotIndexRepository.UpdateAsync(latestMarketData);
                await AddOrUpdateTradePairIndexAsync(latestMarketData);
            }
        }

        public async Task UpdateTradeRecordAsync(string chainId, Guid tradePairId, DateTime timestamp, double volume,
            double tradeValue)
        {
            var lockName = string.Format("{0}-{1}-{2}", chainId,
                tradePairId, GetSnapshotTime(timestamp));
            await using var handle = await _distributedLock.TryAcquireAsync(lockName);
            var value = await _tradeRecordAccumulationCache.GetAsync(lockName);

            if (value == null)
            {
                await _tradeRecordAccumulationCache.SetAsync(lockName, new UpdateTradeRecordBatch()
                {
                    CreateTime = DateTime.UtcNow,
                    ChanId = chainId,
                    TradePairId = tradePairId,
                    Timestamp = timestamp,
                    Volume = volume,
                    TradeValue = tradeValue,
                    TradeCount = 1,
                });
            }
            else
            {
                value.Volume += volume;
                value.TradeValue += tradeValue;
                value.TradeCount += 1;
                if (value.TradeCount >= _tradeRecordOptions.BatchFlushCount)
                {
                    await _updateTradeRecordAsync(chainId, tradePairId, timestamp, value.Volume, value.TradeValue,
                        value.TradeCount);
                    await _tradeRecordAccumulationCache.RemoveAsync(lockName);
                }
                else
                {
                    await _tradeRecordAccumulationCache.SetAsync(lockName, value);
                }
            }
        }

        public async Task _updateTradeRecordAsync(string chainId, Guid tradePairId, DateTime timestamp, double volume,
            double tradeValue, int tradeCount)
        {
            _logger.LogInformation(
                "_updateTradeRecordAsync start.chainId:{chainId},tradePairId:{tradePairId},timestamp:{timestamp},volume:{volume},tradeValue:{tradeValue},tradeCount:{tradeCount}",
                chainId, tradePairId, timestamp, volume, tradeValue, tradeCount);
            var snapshotTime = GetSnapshotTime(timestamp);
            var marketData = await GetTradePairMarketDataIndexAsync(chainId, tradePairId, snapshotTime);

            var tradeAddressCount24H = await _tradeRecordAppService.GetUserTradeAddressCountAsync(chainId,
                tradePairId,
                GetSnapshotTime(timestamp).AddDays(-1), timestamp);

            if (marketData == null)
            {
                var lastMarketData =
                    await GetLatestTradePairMarketDataIndexAsync(chainId, tradePairId, snapshotTime);
                BigDecimal totalSupply = 0;
                if (lastMarketData != null)
                {
                    totalSupply += BigDecimal.Parse(lastMarketData.TotalSupply);
                }

                marketData = new Index.TradePairMarketDataSnapshot()
                {
                    Id = Guid.NewGuid(),
                    ChainId = chainId,
                    TradePairId = tradePairId,
                    Volume = volume,
                    TradeValue = tradeValue,
                    TradeCount = tradeCount,
                    TradeAddressCount24h = tradeAddressCount24H,
                    Timestamp = snapshotTime,
                    TotalSupply = totalSupply.ToNormalizeString()
                };
                if (lastMarketData != null)
                {
                    marketData.Price = lastMarketData.Price;
                    marketData.PriceUSD = lastMarketData.PriceUSD;
                    marketData.TVL = lastMarketData.TVL;
                    marketData.ValueLocked0 = lastMarketData.ValueLocked0;
                    marketData.ValueLocked1 = lastMarketData.ValueLocked1;
                }

                await _snapshotIndexRepository.AddAsync(marketData);
                await AddOrUpdateTradePairIndexAsync(marketData);
            }
            else
            {
                marketData.Volume += volume;
                marketData.TradeValue += tradeValue;
                marketData.TradeCount += tradeCount;
                marketData.TradeAddressCount24h = tradeAddressCount24H;
                await _snapshotIndexRepository.UpdateAsync(marketData);
                await AddOrUpdateTradePairIndexAsync(marketData);
            }
        }

        public async Task UpdateLiquidityAsync(string chainId, Guid tradePairId, DateTime timestamp, double price,
            double priceUSD, double tvl,
            double valueLocked0, double valueLocked1)
        {
            var lockName = string.Format("{0}-{1}-{2}", chainId,
                tradePairId, GetSnapshotTime(timestamp));
            await using var handle = await _distributedLock.TryAcquireAsync(lockName);
            var snapshotTime = GetSnapshotTime(timestamp);
            var marketData = await GetTradePairMarketDataIndexAsync(chainId, tradePairId, snapshotTime);

            if (marketData == null)
            {
                var lastMarketData =
                    await GetLatestTradePairMarketDataIndexAsync(chainId, tradePairId, snapshotTime);
                BigDecimal totalSupply = 0;
                if (lastMarketData != null)
                {
                    totalSupply += BigDecimal.Parse(lastMarketData.TotalSupply);
                }

                marketData = new Index.TradePairMarketDataSnapshot
                {
                    Id = Guid.NewGuid(),
                    ChainId = chainId,
                    TradePairId = tradePairId,
                    Price = price,
                    PriceHigh = price,
                    PriceLow = price,
                    PriceLowUSD = priceUSD,
                    PriceHighUSD = priceUSD,
                    PriceUSD = priceUSD,
                    TVL = tvl,
                    ValueLocked0 = valueLocked0,
                    ValueLocked1 = valueLocked1,
                    Timestamp = snapshotTime,
                    TotalSupply = totalSupply.ToNormalizeString()
                };
                marketData.TradeAddressCount24h =
                    await _tradeRecordAppService.GetUserTradeAddressCountAsync(chainId, tradePairId,
                        timestamp.AddDays(-1), timestamp);
                await _snapshotIndexRepository.AddAsync(marketData);
                await AddOrUpdateTradePairIndexAsync(marketData);
            }
            else
            {
                marketData.Price = price;
                marketData.PriceHigh = Math.Max(marketData.PriceHigh, price);
                marketData.PriceHighUSD = Math.Max(marketData.PriceHighUSD, priceUSD);
                marketData.PriceLow = marketData.PriceLow == 0 ? price : Math.Min(marketData.PriceLow, price);
                marketData.PriceLowUSD =
                    marketData.PriceLowUSD == 0 ? price : Math.Min(marketData.PriceLowUSD, priceUSD);
                marketData.PriceUSD = priceUSD;
                marketData.TVL = tvl;
                marketData.ValueLocked0 = valueLocked0;
                marketData.ValueLocked1 = valueLocked1;

                await _snapshotIndexRepository.UpdateAsync(marketData);
                await AddOrUpdateTradePairIndexAsync(marketData);
            }
        }

        private async Task<Index.TradePairMarketDataSnapshot> GetLatestTradePairMarketDataIndexAsync(string chainId,
            Guid tradePairId, DateTime maxTime)
        {
            return await _snapshotIndexRepository.GetAsync(
                q => q.Term(i => i.Field(f => f.ChainId).Value(chainId))
                     && q.Term(i => i.Field(f => f.TradePairId).Value(tradePairId))
                     && q.DateRange(i => i.Field(f => f.Timestamp).LessThanOrEquals(maxTime)),
                sortExp: s => s.Timestamp, sortType: SortOrder.Descending);
        }

        public async Task<TradePairMarketDataSnapshot> GetLatestTradePairMarketDataAsync(string chainId,
            Guid tradePairId)
        {
            var result = await GetLatestTradePairMarketDataIndexAsync(chainId, tradePairId);
            return _objectMapper.Map<Index.TradePairMarketDataSnapshot, TradePairMarketDataSnapshot>(result);
        }

        public DateTime GetSnapshotTime(DateTime time)
        {
            return time.Date.AddHours(time.Hour);
        }

        public async Task<Index.TradePairMarketDataSnapshot> GetTradePairMarketDataIndexAsync(string chainId,
            Guid tradePairId, DateTime snapshotTime)
        {
            return await _snapshotIndexRepository.GetAsync(
                q => q.Term(i => i.Field(f => f.ChainId).Value(chainId))
                     && q.Term(i => i.Field(f => f.TradePairId).Value(tradePairId))
                     && q.Term(i => i.Field(f => f.Timestamp).Value(snapshotTime)));
        }


        public async Task<Index.TradePairMarketDataSnapshot> GetLatestTVLTradePairMarketDataIndexAsync(string chainId,
            Guid tradePairId, DateTime snapshotTime)
        {
            return await _snapshotIndexRepository.GetAsync(q =>
                    q.Bool(i =>
                        i.Must(f =>
                            f.Range(i =>
                                i.Field(f => f.TVL).GreaterThan(0)))),
                sortExp: s => s.Timestamp, sortType: SortOrder.Descending);
        }


        public async Task<Index.TradePairMarketDataSnapshot> GetLatestTradePairMarketDataIndexAsync(string chainId,
            Guid tradePairId)
        {
            return await _snapshotIndexRepository.GetAsync(q =>
                    q.Term(i => i.Field(f => f.ChainId).Value(chainId)) &&
                    q.Term(i => i.Field(f => f.TradePairId).Value(tradePairId)),
                sortExp: s => s.Timestamp, sortType: SortOrder.Descending);
        }


        public async Task<List<Index.TradePairMarketDataSnapshot>> GetIndexListAsync(string chainId, Guid tradePairId,
            DateTime? timestampMin = null, DateTime? timestampMax = null)
        {
            var mustQuery =
                new List<Func<QueryContainerDescriptor<Index.TradePairMarketDataSnapshot>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.TradePairId).Value(tradePairId)));

            if (timestampMin != null)
            {
                mustQuery.Add(q => q.DateRange(i =>
                    i.Field(f => f.Timestamp)
                        .GreaterThanOrEquals(timestampMin.Value)));
            }

            if (timestampMax != null)
            {
                mustQuery.Add(q => q.DateRange(i =>
                    i.Field(f => f.Timestamp)
                        .LessThan(timestampMax)));
            }

            QueryContainer Filter(QueryContainerDescriptor<Index.TradePairMarketDataSnapshot> f) =>
                f.Bool(b => b.Must(mustQuery));

            var list = await _snapshotIndexRepository.GetListAsync(Filter);
            return list.Item2;
        }

        private async Task AddOrUpdateTradePairIndexAsync(Index.TradePairMarketDataSnapshot snapshotDto)
        {
            var latestSnapshot =
                await GetLatestTradePairMarketDataIndexAsync(snapshotDto.ChainId,
                    snapshotDto.TradePairId);

            if (latestSnapshot != null && snapshotDto.Timestamp < latestSnapshot.Timestamp)
            {
                return;
            }

            var snapshots = await GetIndexListAsync(snapshotDto.ChainId,
                snapshotDto.TradePairId, snapshotDto.Timestamp.AddDays(-2));
            var volume24h = 0d;
            var tradeValue24h = 0d;
            var tradeCount24h = 0;
            var priceHigh24h = snapshotDto.PriceHigh;
            var priceLow24h = snapshotDto.PriceLow;
            var priceHigh24hUSD = snapshotDto.PriceHighUSD;
            var priceLow24hUSD = snapshotDto.PriceLowUSD;

            var daySnapshot = snapshots.Where(s => s.Timestamp >= snapshotDto.Timestamp.AddDays(-1)).ToList();
            foreach (var snapshot in daySnapshot)
            {
                volume24h += snapshot.Volume;
                tradeValue24h += snapshot.TradeValue;
                tradeCount24h += snapshot.TradeCount;

                if (priceLow24h == 0)
                {
                    priceLow24h = snapshot.PriceLow;
                }

                if (snapshot.PriceLow != 0)
                {
                    priceLow24h = Math.Min(priceLow24h, snapshot.PriceLow);
                }

                if (priceLow24hUSD == 0)
                {
                    priceLow24hUSD = snapshot.PriceLowUSD;
                }

                if (snapshot.PriceLowUSD != 0)
                {
                    priceLow24hUSD = Math.Min(priceLow24hUSD, snapshot.PriceLowUSD);
                }

                priceHigh24hUSD = Math.Max(priceHigh24hUSD, snapshot.PriceHighUSD);
                priceHigh24h = Math.Max(priceHigh24h, snapshot.PriceHigh);
            }

            var lastDaySnapshot = snapshots.Where(s => s.Timestamp < snapshotDto.Timestamp.AddDays(-1))
                .OrderByDescending(s => s.Timestamp).ToList();
            var lastDayVolume24h = lastDaySnapshot.Sum(snapshot => snapshot.Volume);
            var lastDayTvl = 0d;
            var lastDayPriceUSD = 0d;

            if (lastDaySnapshot.Count > 0)
            {
                var snapshot = lastDaySnapshot.First();
                lastDayTvl = snapshot.TVL;
                lastDayPriceUSD = snapshot.PriceUSD;
            }
            else
            {
                var snapshot = GetLatestTVLTradePairMarketDataIndexAsync(snapshotDto.ChainId, snapshotDto.TradePairId,
                    snapshotDto.Timestamp);
                if (snapshot != null && snapshot.Result != null)
                {
                    lastDayTvl = snapshot.Result.TVL;
                    lastDayPriceUSD = snapshot.Result.PriceUSD;
                }
            }


            var existIndex = await _tradePairIndexRepository.GetAsync(snapshotDto.TradePairId);

            existIndex.TotalSupply = snapshotDto.TotalSupply;
            existIndex.Price = snapshotDto.Price;
            existIndex.PriceUSD = snapshotDto.PriceUSD;
            existIndex.TVL = snapshotDto.TVL;
            existIndex.ValueLocked0 = snapshotDto.ValueLocked0;
            existIndex.ValueLocked1 = snapshotDto.ValueLocked1;
            existIndex.Volume24h = volume24h;
            existIndex.TradeValue24h = tradeValue24h;
            existIndex.TradeCount24h = tradeCount24h;
            existIndex.TradeAddressCount24h = snapshotDto.TradeAddressCount24h;
            existIndex.PriceHigh24h = priceHigh24h;
            existIndex.PriceLow24h = priceLow24h;
            existIndex.PriceHigh24hUSD = priceHigh24hUSD;
            existIndex.PriceLow24hUSD = priceLow24hUSD;
            existIndex.PriceChange24h = lastDayPriceUSD == 0
                ? 0
                : existIndex.PriceUSD - lastDayPriceUSD;
            existIndex.PricePercentChange24h = lastDayPriceUSD == 0
                ? 0
                : (existIndex.PriceUSD - lastDayPriceUSD) * 100 / lastDayPriceUSD;
            existIndex.VolumePercentChange24h = lastDayVolume24h == 0
                ? 0
                : (existIndex.Volume24h - lastDayVolume24h) * 100 / lastDayVolume24h;
            existIndex.TVLPercentChange24h = lastDayTvl == 0
                ? 0
                : (existIndex.TVL - lastDayTvl) * 100 / lastDayTvl;

            if (snapshotDto.TVL != 0)
            {
                var volume7d = (await GetIndexListAsync(snapshotDto.ChainId,
                        snapshotDto.TradePairId, snapshotDto.Timestamp.AddDays(-7), snapshotDto.Timestamp))
                    .Sum(k => k.Volume);
                volume7d += snapshotDto.Volume;
                existIndex.FeePercent7d = (volume7d * snapshotDto.PriceUSD * existIndex.FeeRate * 365 * 100) /
                                          (snapshotDto.TVL * 7);
            }


            _logger.LogInformation("whx AddOrUpdateTradePairIndex: " + JsonConvert.SerializeObject(existIndex));


            await _tradePairIndexRepository.AddOrUpdateAsync(existIndex);
            await _bus.Publish(new NewIndexEvent<TradePairIndexDto>
            {
                Data = _objectMapper.Map<Index.TradePair, TradePairIndexDto>(existIndex)
            });
        }


        public class UpdateTotalSupplyBatch
        {
            public DateTime LastTime { get; set; }
            public string ChanId { get; set; }
            public Guid TradePairId { get; set; }
            public DateTime Timestamp { get; set; }
            public string TotalSupply { get; set; }
        }

        public class CacheKeys
        {
            HashSet<string> Set { get; set; }
        }

        public class UpdateTradeRecordBatch
        {
            public DateTime CreateTime { get; set; }
            public string ChanId { get; set; }

            public BigDecimal TotalSupply { get; set; }
            public Guid TradePairId { get; set; }
            public DateTime Timestamp { get; set; }
            public double Volume { get; set; }
            public double TradeValue { get; set; }
            public int TradeAddressCount24h { get; set; }
            public int TradeCount { get; set; }
        }

        public class UpdateLiquidityBatch
        {
            public DateTime CreateTime { get; set; }
            public string ChainId { get; set; }
            public Guid TradePairId { get; set; }
            public DateTime Timestamp { get; set; }
            public double Price { get; set; }

            public double PriceHigh { get; set; }

            public double PriceHighUSD { get; set; }

            public double PriceLow { get; set; }

            public double PriceLowUSD { get; set; }

            public double PriceUSD { get; set; }

            public double TVL { get; set; }

            public double ValueLocked0 { get; set; }

            public double ValueLocked1 { get; set; }
        }
    }
}