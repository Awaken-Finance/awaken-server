using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Etos;
using MassTransit;
using Microsoft.Extensions.Logging;
using Nethereum.Util;
using Orleans;

using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;
using JsonConvert = Newtonsoft.Json.JsonConvert;
using IObjectMapper = Volo.Abp.ObjectMapping.IObjectMapper;

using IndexTradePair = AwakenServer.Trade.Index.TradePair;
using TradePair = AwakenServer.Trade.TradePair;
using IndexTradePairMarketDataSnapshot = AwakenServer.Trade.Index.TradePairMarketDataSnapshot;
using TradePairMarketDataSnapshot = AwakenServer.Trade.TradePairMarketDataSnapshot;

namespace AwakenServer.EntityHandler.Trade
{
    public class TradePairMarketDataIndexHandler : TradeIndexHandlerBase,
        IDistributedEventHandler<EntityCreatedEto<TradePairMarketDataSnapshotEto>>,
        IDistributedEventHandler<EntityUpdatedEto<TradePairMarketDataSnapshotEto>>
    {
        private readonly INESTRepository<IndexTradePairMarketDataSnapshot, Guid> _snapshotIndexRepository;
        private readonly INESTRepository<IndexTradePair, Guid> _tradePairIndexRepository;
        private readonly ITradePairMarketDataProvider _tradePairMarketDataProvider;
        private readonly IBus _bus;
        private readonly IClusterClient _clusterClient;
        private readonly ILogger<TradePairMarketDataIndexHandler> _logger;
        private readonly IObjectMapper _objectMapper;
        
        public TradePairMarketDataIndexHandler(
            INESTRepository<IndexTradePairMarketDataSnapshot, Guid> snapshotIndexRepository,
            INESTRepository<IndexTradePair, Guid> tradePairIndexRepository,
            ITradePairMarketDataProvider tradePairMarketDataProvider,
            IBus bus,
            IClusterClient clusterClient,
            ILogger<TradePairMarketDataIndexHandler> logger,
            IObjectMapper objectMapper)
        {
            _snapshotIndexRepository = snapshotIndexRepository;
            _tradePairIndexRepository = tradePairIndexRepository;
            _tradePairMarketDataProvider = tradePairMarketDataProvider;
            _bus = bus;
            _clusterClient = clusterClient;
            _logger = logger;
            _objectMapper = objectMapper;
        }

        public async Task HandleEventAsync(EntityCreatedEto<TradePairMarketDataSnapshotEto> eventData)
        {
            await AddOrUpdateIndexAsync(eventData.Entity);
        }

        public async Task HandleEventAsync(EntityUpdatedEto<TradePairMarketDataSnapshotEto> eventData)
        {
            await AddOrUpdateIndexAsync(eventData.Entity);
        }

        private async Task AddOrUpdateIndexAsync(TradePairMarketDataSnapshotEto snapshotEto)
        {
            var index = ObjectMapper.Map<TradePairMarketDataSnapshotEto, IndexTradePairMarketDataSnapshot>(snapshotEto);
            await _snapshotIndexRepository.AddOrUpdateAsync(index);
        }

        
    }
}