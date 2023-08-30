using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Etos;
using AwakenServer.Trade.Index;
using MassTransit;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;

namespace AwakenServer.EntityHandler.Trade
{
    public class TradePairIndexHandler : TradeIndexHandlerBase,
        IDistributedEventHandler<EntityCreatedEto<TradePairEto>>
    {
        private readonly INESTRepository<TradePair, Guid> _tradePairIndexRepository;
        private readonly IBus _bus;
        
        public TradePairIndexHandler(INESTRepository<TradePair, Guid> tradePairIndexRepository, IBus bus)
        {
            _tradePairIndexRepository = tradePairIndexRepository;
            _bus = bus;
        }

        public async Task HandleEventAsync(EntityCreatedEto<TradePairEto> eventData)
        {
            var index = ObjectMapper.Map<TradePairEto, TradePair>(eventData.Entity);
            index.Token0 = await GetTokenAsync(eventData.Entity.Token0Id);
            index.Token1 = await GetTokenAsync(eventData.Entity.Token1Id);

            await _tradePairIndexRepository.AddOrUpdateAsync(index);
            await _bus.Publish<NewIndexEvent<TradePairIndexDto>>(new NewIndexEvent<TradePairIndexDto>
            {
                Data = ObjectMapper.Map<TradePair, TradePairIndexDto>(index)
            });
            /*await DistributedEventBus.PublishAsync(new NewIndexEvent<TradePairIndexDto>
            {
                Data = ObjectMapper.Map<TradePair, TradePairIndexDto>(index)
            });*/
        }
    }
}