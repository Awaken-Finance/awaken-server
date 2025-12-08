using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Trade.Etos;
using AwakenServer.Trade.Index;
using Volo.Abp.EventBus.Distributed;

namespace AwakenServer.EntityHandler.Trade
{
    public class UserTradeSummaryIndexHandler : TradeIndexHandlerBase,
        IDistributedEventHandler<UserTradeSummaryEto>
    {
        private readonly INESTRepository<UserTradeSummary, Guid> _userTradeSummaryIndexRepository;

        public UserTradeSummaryIndexHandler(INESTRepository<UserTradeSummary, Guid> userTradeSummaryIndexRepository)
        {
            _userTradeSummaryIndexRepository = userTradeSummaryIndexRepository;
        }

        public async Task HandleEventAsync(UserTradeSummaryEto eventData)
        {
            await _userTradeSummaryIndexRepository.AddOrUpdateAsync(ObjectMapper.Map<UserTradeSummaryEto, UserTradeSummary>(eventData));
        }
    }
}