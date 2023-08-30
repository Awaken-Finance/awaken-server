using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Etos;
using MassTransit;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;
using TradePair = AwakenServer.Trade.Index.TradePair;
using TradePairMarketDataSnapshot = AwakenServer.Trade.Index.TradePairMarketDataSnapshot;

namespace AwakenServer.EntityHandler.Trade
{
    public class TradePairMarketDataIndexHandler : TradeIndexHandlerBase,
        IDistributedEventHandler<EntityCreatedEto<TradePairMarketDataSnapshotEto>>,
        IDistributedEventHandler<EntityUpdatedEto<TradePairMarketDataSnapshotEto>>
    {
        private readonly INESTRepository<TradePairMarketDataSnapshot, Guid> _snapshotIndexRepository;
        private readonly INESTRepository<TradePair, Guid> _tradePairIndexRepository;
        private readonly ITradePairMarketDataProvider _tradePairMarketDataProvider;
        private readonly IBus _bus;

        public TradePairMarketDataIndexHandler(
            INESTRepository<TradePairMarketDataSnapshot, Guid> snapshotIndexRepository,
            INESTRepository<TradePair, Guid> tradePairIndexRepository,
            ITradePairMarketDataProvider tradePairMarketDataProvider,
            IBus bus)
        {
            _snapshotIndexRepository = snapshotIndexRepository;
            _tradePairIndexRepository = tradePairIndexRepository;
            _tradePairMarketDataProvider = tradePairMarketDataProvider;
            _bus = bus;
        }

        public async Task HandleEventAsync(EntityCreatedEto<TradePairMarketDataSnapshotEto> eventData)
        {
            await AddOrUpdateIndexAsync(eventData.Entity);
            await AddOrUpdateTradePairIndexAsync(eventData.Entity);
        }

        public async Task HandleEventAsync(EntityUpdatedEto<TradePairMarketDataSnapshotEto> eventData)
        {
            await AddOrUpdateIndexAsync(eventData.Entity);
            await AddOrUpdateTradePairIndexAsync(eventData.Entity);
        }

        private async Task AddOrUpdateIndexAsync(TradePairMarketDataSnapshotEto snapshotEto)
        {
            var index = ObjectMapper.Map<TradePairMarketDataSnapshotEto, TradePairMarketDataSnapshot>(snapshotEto);
            await _snapshotIndexRepository.AddOrUpdateAsync(index);
        }

        private async Task AddOrUpdateTradePairIndexAsync(TradePairMarketDataSnapshotEto snapshotEto)
        {
            var latestSnapshot =
                await _tradePairMarketDataProvider.GetLatestTradePairMarketDataIndexAsync(snapshotEto.ChainId,
                    snapshotEto.TradePairId);
            if (latestSnapshot != null && snapshotEto.Timestamp < latestSnapshot.Timestamp)
            {
                return;
            }

            var snapshots = await _tradePairMarketDataProvider.GetIndexListAsync(snapshotEto.ChainId,
                snapshotEto.TradePairId, snapshotEto.Timestamp.AddDays(-2), snapshotEto.Timestamp);

            var volume24h = snapshotEto.Volume;
            var tradeValue24h = snapshotEto.TradeValue;
            var tradeCount24h = snapshotEto.TradeCount;
            var priceHigh24h = snapshotEto.PriceHigh;
            var priceLow24h = snapshotEto.PriceLow;
            var priceHigh24hUSD = snapshotEto.PriceHighUSD;
            var priceLow24hUSD = snapshotEto.PriceLowUSD;

            var daySnapshot = snapshots.Where(s=>s.Timestamp >= snapshotEto.Timestamp.AddDays(-1)).ToList();
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

            var lastDaySnapshot = snapshots.Where(s => s.Timestamp < snapshotEto.Timestamp.AddDays(-1))
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

            var existIndex = await _tradePairIndexRepository.GetAsync(snapshotEto.TradePairId);

            existIndex.TotalSupply = snapshotEto.TotalSupply;
            existIndex.Price = snapshotEto.Price;
            existIndex.PriceUSD = snapshotEto.PriceUSD;
            existIndex.TVL = snapshotEto.TVL;
            existIndex.ValueLocked0 = snapshotEto.ValueLocked0;
            existIndex.ValueLocked1 = snapshotEto.ValueLocked1;
            existIndex.Volume24h = volume24h;
            existIndex.TradeValue24h = tradeValue24h;
            existIndex.TradeCount24h = tradeCount24h;
            existIndex.TradeAddressCount24h = snapshotEto.TradeAddressCount24h;
            existIndex.PriceHigh24h = priceHigh24h;
            existIndex.PriceLow24h = priceLow24h;
            existIndex.PriceHigh24hUSD = priceHigh24hUSD;
            existIndex.PriceLow24hUSD = priceLow24hUSD;
            existIndex.PricePercentChange24h = lastDayPriceUSD == 0
                ? 0
                : (existIndex.PriceUSD - lastDayPriceUSD) * 100 / lastDayPriceUSD;
            existIndex.VolumePercentChange24h = lastDayVolume24h == 0
                ? 0
                : (existIndex.Volume24h - lastDayVolume24h) * 100 / lastDayVolume24h;
            existIndex.TVLPercentChange24h = lastDayTvl == 0
                ? 0
                : (existIndex.TVL - lastDayTvl) * 100 / lastDayTvl;

            if (snapshotEto.TVL != 0)
            {
                var volume7d = (await _tradePairMarketDataProvider.GetIndexListAsync(snapshotEto.ChainId,
                        snapshotEto.TradePairId, snapshotEto.Timestamp.AddDays(-7), snapshotEto.Timestamp))
                    .Sum(k => k.Volume);
                volume7d += snapshotEto.Volume;
                existIndex.FeePercent7d = (volume7d * snapshotEto.PriceUSD * existIndex.FeeRate * 365 * 100) /
                                          (snapshotEto.TVL * 7);
            }

            await _tradePairIndexRepository.AddOrUpdateAsync(existIndex);

            await _bus.Publish<NewIndexEvent<TradePairIndexDto>>(new NewIndexEvent<TradePairIndexDto>
            {
                Data = ObjectMapper.Map<TradePair, TradePairIndexDto>(existIndex)
            });
            /*await DistributedEventBus.PublishAsync(new NewIndexEvent<TradePairIndexDto>
            {
                Data = ObjectMapper.Map<TradePair, TradePairIndexDto>(existIndex)
            });*/
        }
    }
}