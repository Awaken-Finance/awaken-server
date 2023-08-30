using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Trade.Etos;
using AwakenServer.Trade.Index;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;

namespace AwakenServer.EntityHandler.Trade
{
    public class LiquidityRecordIndexHandler : TradeIndexHandlerBase,
        IDistributedEventHandler<EntityCreatedEto<LiquidityRecordEto>>
    {
        private readonly INESTRepository<LiquidityRecord, Guid> _liquidityRecordIndexRepository;

        public LiquidityRecordIndexHandler(INESTRepository<LiquidityRecord, Guid> liquidityRecordIndexRepository)
        {
            _liquidityRecordIndexRepository = liquidityRecordIndexRepository;
        }

        public async Task HandleEventAsync(EntityCreatedEto<LiquidityRecordEto> eventData)
        {
            var index = ObjectMapper.Map<LiquidityRecordEto, LiquidityRecord>(eventData.Entity);
            index.TradePair = await GetTradePariWithTokenAsync(eventData.Entity.TradePairId);
            
            await _liquidityRecordIndexRepository.AddOrUpdateAsync(index);
        }
    }
}