using System.Threading.Tasks;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AwakenServer.Trade.Handlers
{
    public class NewTradeRecordHandler : ILocalEventHandler<NewTradeRecordEvent>, ITransientDependency
    {
        private readonly ITradePairMarketDataProvider _tradePairMarketDataProvider;
        private readonly ITradeRecordAppService _tradeRecordAppService;

        public NewTradeRecordHandler(ITradePairMarketDataProvider tradePairMarketDataProvider,
            ITradeRecordAppService tradeRecordAppService, IClusterClient clusterClient)
        {
            _tradePairMarketDataProvider = tradePairMarketDataProvider;
            _tradeRecordAppService = tradeRecordAppService;
        }

        public async Task HandleEventAsync(NewTradeRecordEvent eventData)
        {
            var time = _tradePairMarketDataProvider.GetSnapshotTime(eventData.Timestamp);

            var userTradeCount = await _tradeRecordAppService.GetUserTradeAddressCountAsync(eventData.ChainId, eventData.TradePairId,
                time.AddDays(-1), eventData.Timestamp);
            
            await _tradePairMarketDataProvider.UpdateTradeRecordAsync(eventData.ChainId, eventData.TradePairId,
                eventData.Timestamp, double.Parse(eventData.Token0Amount), double.Parse(eventData.Token1Amount),
                userTradeCount);
        }
    }
}