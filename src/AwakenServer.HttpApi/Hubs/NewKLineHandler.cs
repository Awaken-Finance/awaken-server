using System.Threading.Tasks;
using AwakenServer.Trade.Dtos;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Serilog;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AwakenServer.Hubs
{
    public class NewKLineHandler : IConsumer<NewIndexEvent<KLineDto>>, ITransientDependency
    {
        private readonly IHubContext<TradeHub> _hubContext;
        private readonly ITradeHubGroupProvider _tradeHubGroupProvider;
        private readonly ILogger<NewKLineHandler> _logger;


        public NewKLineHandler(IHubContext<TradeHub> hubContext, ITradeHubGroupProvider tradeHubGroupProvider,
            ILogger<NewKLineHandler> logger)
        {
            _hubContext = hubContext;
            _tradeHubGroupProvider = tradeHubGroupProvider;
            _logger = logger;
        }


        public async Task Consume(ConsumeContext<NewIndexEvent<KLineDto>> eventData)
        {
            var klineGroupName =
                _tradeHubGroupProvider.GetKlineGroupName(eventData.Message.Data.ChainId,
                    eventData.Message.Data.TradePairId, eventData.Message.Data.Period);
            Log.Information(
                "NewKLineHandler: Consume KLineDto:klineGroupName:{klineGroupName},Period:{period},Timestamp:{timestamp}",
                klineGroupName, eventData.Message.Data.Period, eventData.Message.Data.Timestamp);
            await _hubContext.Clients.Group(klineGroupName).SendAsync("ReceiveKline", eventData.Message.Data);
        }
    }
}