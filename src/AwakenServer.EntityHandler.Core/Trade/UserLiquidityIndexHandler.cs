using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Trade.Etos;
using AwakenServer.Trade.Index;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;

namespace AwakenServer.EntityHandler.Trade
{
    public class UserLiquidityIndexHandler : TradeIndexHandlerBase,
        IDistributedEventHandler<EntityCreatedEto<UserLiquidityEto>>,
        IDistributedEventHandler<EntityUpdatedEto<UserLiquidityEto>>
    {
        private readonly INESTRepository<UserLiquidity, Guid> _userLiquidityIndexRepository;

        public UserLiquidityIndexHandler(INESTRepository<UserLiquidity, Guid> userLiquidityIndexRepository)
        {
            _userLiquidityIndexRepository = userLiquidityIndexRepository;
        }

        public async Task HandleEventAsync(EntityCreatedEto<UserLiquidityEto> eventData)
        {
            await AddOrUpdateIndexAsync(eventData.Entity);
        }

        public async Task HandleEventAsync(EntityUpdatedEto<UserLiquidityEto> eventData)
        {
            await AddOrUpdateIndexAsync(eventData.Entity);
        }

        private async Task AddOrUpdateIndexAsync(UserLiquidityEto eto)
        {
            var index = ObjectMapper.Map<UserLiquidityEto, UserLiquidity>(eto);
            index.TradePair = await GetTradePariWithTokenAsync(eto.TradePairId);
            
            await _userLiquidityIndexRepository.AddOrUpdateAsync(index);
        }
    }
}