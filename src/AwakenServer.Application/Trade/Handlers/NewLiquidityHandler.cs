using System.Threading.Tasks;
using Nethereum.Util;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AwakenServer.Trade.Handlers
{
    public class NewLiquidityHandler : ILocalEventHandler<NewLiquidityRecordEvent>, ITransientDependency
    {
        private readonly ITradePairMarketDataProvider _tradePairMarketDataProvider;

        public NewLiquidityHandler(ITradePairMarketDataProvider tradePairMarketDataProvider)
        {
            _tradePairMarketDataProvider = tradePairMarketDataProvider;
        }

        public async Task HandleEventAsync(NewLiquidityRecordEvent eventData)
        {
            var lpAmount = BigDecimal.Parse(eventData.LpTokenAmount);
            lpAmount = eventData.Type == LiquidityType.Mint ? lpAmount : -lpAmount;
            await _tradePairMarketDataProvider.UpdateTotalSupplyAsync(eventData.ChainId, eventData.TradePairId,
                eventData.Timestamp, lpAmount);
        }
    }
}