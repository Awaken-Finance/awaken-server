using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Etos;
using MassTransit;
using MongoDB.Bson;
using Orleans;
using Serilog;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;
using TradePair = AwakenServer.Trade.Index.TradePair;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace AwakenServer.EntityHandler.Trade
{
    public class TradePairIndexHandler : TradeIndexHandlerBase,
        IDistributedEventHandler<EntityCreatedEto<TradePairEto>>
    {
        private readonly INESTRepository<TradePairInfoIndex, Guid> _tradePairInfoIndex;
        private readonly INESTRepository<TradePair, Guid> _tradePairIndexRepository;
        private readonly IBus _bus;
        private readonly IClusterClient _clusterClient;
        private readonly ILogger _logger;
        
        public TradePairIndexHandler(INESTRepository<TradePair, Guid> tradePairIndexRepository,
            IBus bus,
            IClusterClient clusterClient)
        {
            _tradePairIndexRepository = tradePairIndexRepository;
            _bus = bus;
            _clusterClient = clusterClient;
            _logger = Log.ForContext<TradePairIndexHandler>();
        }

        public async Task HandleEventAsync(EntityCreatedEto<TradePairEto> eventData)
        {
            var index = ObjectMapper.Map<TradePairEto, TradePair>(eventData.Entity);
            index.Token0 = await GetTokenAsync(eventData.Entity.ChainId, eventData.Entity.Token0Symbol);
            index.Token1 = await GetTokenAsync(eventData.Entity.ChainId, eventData.Entity.Token1Symbol);

            await _tradePairIndexRepository.AddOrUpdateAsync(index);
            
            await _bus.Publish<NewIndexEvent<TradePairIndexDto>>(new NewIndexEvent<TradePairIndexDto>
            {
                Data = ObjectMapper.Map<TradePair, TradePairIndexDto>(index)
            });
            
            _logger.Information($"write trade pair to es, {JsonConvert.SerializeObject(index)}");
        }
    }
}