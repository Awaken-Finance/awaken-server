using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Etos;
using MassTransit;
using Orleans;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;

namespace AwakenServer.EntityHandler.Trade
{
    public class TradePairInfoIndexHandler : TradeIndexHandlerBase,
        IDistributedEventHandler<EntityCreatedEto<TradePairInfoEto>>
    {
        private readonly INESTRepository<TradePairInfoIndex, Guid> _tradePairInfoIndex;
        
        public TradePairInfoIndexHandler(INESTRepository<TradePairInfoIndex, Guid> tradePairInfoIndex)
        {
            _tradePairInfoIndex = tradePairInfoIndex;
        }

        public async Task HandleEventAsync(EntityCreatedEto<TradePairInfoEto> eventData)
        {
            await _tradePairInfoIndex.AddOrUpdateAsync(ObjectMapper.Map<TradePairInfoEto, TradePairInfoIndex>(eventData.Entity));
        }
    }
}