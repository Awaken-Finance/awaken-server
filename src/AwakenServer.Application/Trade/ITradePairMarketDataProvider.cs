using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Trade.Dtos;
using MassTransit;
using Nest;
using Nethereum.Util;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Trade
{
    public interface ITradePairMarketDataProvider
    {
        Task UpdateTotalSupplyAsync(string chainId, Guid tradePairId, DateTime timestamp, BigDecimal lpTokenAmount);

        Task UpdateTradeRecordAsync(string chainId, Guid tradePairId, DateTime timestamp, double volume, double tradeValue, int tradeAddressCount24h);

        Task UpdateLiquidityAsync(string chainId, Guid tradePairId, DateTime timestamp, double price, double priceUSD,
            double tvl, double valueLocked0, double valueLocked1);

        Task<TradePairMarketDataSnapshot> GetLatestTradePairMarketDataAsync(string chainId, Guid tradePairId);

        Task<Index.TradePairMarketDataSnapshot> GetTradePairMarketDataIndexAsync(string chainId, Guid tradePairId,
            DateTime snapshotTime);

        DateTime GetSnapshotTime(DateTime time);

        Task<Index.TradePairMarketDataSnapshot> GetLatestTradePairMarketDataIndexAsync(string chainId, Guid tradePairId);

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
        
        public TradePairMarketDataProvider(INESTRepository<Index.TradePairMarketDataSnapshot, Guid> snapshotIndexRepository,
            INESTRepository<Index.TradePair, Guid> tradePairIndexRepository, 
            ITradeRecordAppService tradeRecordAppService, 
            IDistributedEventBus distributedEventBus,
            IBus bus,
            IObjectMapper objectMapper)
        {
            _snapshotIndexRepository = snapshotIndexRepository;
            _tradePairIndexRepository = tradePairIndexRepository;
            _tradeRecordAppService = tradeRecordAppService;
            _distributedEventBus = distributedEventBus;
            _objectMapper = objectMapper;
            _bus = bus;
        }

        public async Task UpdateTotalSupplyAsync(string chainId, Guid tradePairId, DateTime timestamp, BigDecimal lpTokenAmount)
        {
            var snapshotTime = GetSnapshotTime(timestamp);
            var marketData = await GetTradePairMarketDataIndexAsync(chainId, tradePairId, snapshotTime);
            
            if (marketData == null)
            {
                var lastMarketData = await GetLatestTradePairMarketDataIndexAsync(chainId, tradePairId, snapshotTime);
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
                    TotalSupply = totalSupply.ToNormalizeString(),
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
                    await _tradeRecordAppService.GetUserTradeAddressCountAsync(chainId, tradePairId, timestamp.AddDays(-1), timestamp);
                await _snapshotIndexRepository.AddAsync(marketData);
                await AddOrUpdateTradePairIndexAsync(marketData);
            }
            else
            {
                var totalSupply = BigDecimal.Parse(marketData.TotalSupply);
                marketData.TotalSupply = (totalSupply + lpTokenAmount).ToNormalizeString();
                await _snapshotIndexRepository.UpdateAsync(marketData);
                await AddOrUpdateTradePairIndexAsync(marketData);
            }

            var latestMarketData = await GetLatestTradePairMarketDataIndexAsync(chainId, tradePairId);
            if (latestMarketData != null && latestMarketData.Timestamp > snapshotTime)
            {
                latestMarketData.TotalSupply = (BigDecimal.Parse(latestMarketData.TotalSupply) + lpTokenAmount).ToNormalizeString();
                await _snapshotIndexRepository.UpdateAsync(latestMarketData);
                await AddOrUpdateTradePairIndexAsync(latestMarketData);
            }
        }

        public async Task UpdateTradeRecordAsync(string chainId, Guid tradePairId, DateTime timestamp, double volume, double tradeValue, int tradeAddressCount24h)
        {
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
                
                marketData = new Index.TradePairMarketDataSnapshot()
                {
                    Id = Guid.NewGuid(),
                    ChainId = chainId,
                    TradePairId = tradePairId,
                    Volume = volume,
                    TradeValue = tradeValue,
                    TradeCount = 1,
                    TradeAddressCount24h = tradeAddressCount24h,
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
                marketData.TradeCount += 1;
                marketData.TradeAddressCount24h = tradeAddressCount24h;
                
                await _snapshotIndexRepository.UpdateAsync(marketData);
                await AddOrUpdateTradePairIndexAsync(marketData);
            }
        }

        public async Task UpdateLiquidityAsync(string chainId, Guid tradePairId, DateTime timestamp, double price, double priceUSD, double tvl,
            double valueLocked0, double valueLocked1)
        {
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
                    await _tradeRecordAppService.GetUserTradeAddressCountAsync(chainId, tradePairId, timestamp.AddDays(-1), timestamp);
                await _snapshotIndexRepository.AddAsync(marketData);
                await AddOrUpdateTradePairIndexAsync(marketData);
            }
            else
            {
                marketData.Price = price;
                marketData.PriceHigh = Math.Max(marketData.PriceHigh, price);
                marketData.PriceHighUSD = Math.Max(marketData.PriceHighUSD, priceUSD);
                marketData.PriceLow = Math.Min(marketData.PriceLow, price);
                marketData.PriceLowUSD = Math.Min(marketData.PriceLowUSD, priceUSD);
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

        public async Task<TradePairMarketDataSnapshot> GetLatestTradePairMarketDataAsync(string chainId, Guid tradePairId)
        {
            var result = await GetLatestTradePairMarketDataIndexAsync(chainId, tradePairId);
            return _objectMapper.Map<Index.TradePairMarketDataSnapshot, TradePairMarketDataSnapshot>(result);
        }

        public DateTime GetSnapshotTime(DateTime time)
        {
            return time.Date.AddHours(time.Hour);
        }
        
        public async Task<Index.TradePairMarketDataSnapshot> GetTradePairMarketDataIndexAsync(string chainId, Guid tradePairId, DateTime snapshotTime)
        {
            return await _snapshotIndexRepository.GetAsync(
                q => q.Term(i => i.Field(f => f.ChainId).Value(chainId))
                     && q.Term(i => i.Field(f => f.TradePairId).Value(tradePairId))
                     && q.Term(i => i.Field(f => f.Timestamp).Value(snapshotTime)));
        }


        public async Task<Index.TradePairMarketDataSnapshot> GetLatestTradePairMarketDataIndexAsync(string chainId, Guid tradePairId)
        {
            return await _snapshotIndexRepository.GetAsync(q =>
                    q.Term(i => i.Field(f => f.ChainId).Value(chainId)) &&
                    q.Term(i => i.Field(f => f.TradePairId).Value(tradePairId)),
                sortExp: s => s.Timestamp, sortType:SortOrder.Descending);
        }

        public async Task<List<Index.TradePairMarketDataSnapshot>> GetIndexListAsync(string chainId, Guid tradePairId, DateTime? timestampMin = null, DateTime? timestampMax = null)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<Index.TradePairMarketDataSnapshot>, QueryContainer>>();
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
            
            QueryContainer Filter(QueryContainerDescriptor<Index.TradePairMarketDataSnapshot> f) => f.Bool(b => b.Must(mustQuery));
            
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
            
            var daySnapshot = snapshots.Where(s=>s.Timestamp >= snapshotDto.Timestamp.AddDays(-1)).ToList();
            foreach (var snapshot in daySnapshot)
            {
                volume24h += snapshot.Volume;
                tradeValue24h += snapshot.TradeValue;
                tradeCount24h += snapshot.TradeCount;
                priceHigh24h = Math.Max(priceHigh24h, snapshot.PriceHigh);
                priceLow24h = Math.Min(priceLow24h, snapshot.PriceLow);
                priceHigh24hUSD = Math.Max(priceHigh24hUSD, snapshot.PriceHighUSD);
                priceLow24hUSD = Math.Min(priceLow24hUSD, snapshot.PriceLowUSD);
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
                var sortDaySnapshot = daySnapshot.OrderBy(s => s.Timestamp).ToList();
                if (sortDaySnapshot.Count > 0)
                {
                    var snapshot = sortDaySnapshot.First();
                    lastDayTvl = snapshot.TVL;
                    lastDayPriceUSD = snapshot.PriceUSD;
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
            existIndex.PriceChange24h  = lastDayPriceUSD == 0
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

            await _tradePairIndexRepository.AddOrUpdateAsync(existIndex);
            await _bus.Publish<NewIndexEvent<TradePairIndexDto>>(new NewIndexEvent<TradePairIndexDto>
            {
                Data = _objectMapper.Map<Index.TradePair, TradePairIndexDto>(existIndex)
            });
            /*await _distributedEventBus.PublishAsync(new NewIndexEvent<TradePairIndexDto>
            {
                Data = _objectMapper.Map<Index.TradePair, TradePairIndexDto>(existIndex)
            });*/
        }
    }
}