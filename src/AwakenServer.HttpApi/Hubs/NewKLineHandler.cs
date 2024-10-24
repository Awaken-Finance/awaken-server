using System.Threading.Tasks;
using AwakenServer.Trade.Dtos;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.Hubs
{
    public class NewKLineHandler : IConsumer<NewIndexEvent<KLineDto>>, ITransientDependency
    {
        private readonly IHubContext<TradeHub> _hubContext;
        private readonly ITradeHubGroupProvider _tradeHubGroupProvider;
        private readonly ILogger _logger;


        public NewKLineHandler(IHubContext<TradeHub> hubContext, ITradeHubGroupProvider tradeHubGroupProvider)
        {
            _hubContext = hubContext;
            _tradeHubGroupProvider = tradeHubGroupProvider;
            _logger = Log.ForContext<NewKLineHandler>();
        }


        public async Task Consume(ConsumeContext<NewIndexEvent<KLineDto>> eventData)
        {
            var klineGroupName =
                _tradeHubGroupProvider.GetKlineGroupName(eventData.Message.Data.ChainId,
                    eventData.Message.Data.TradePairId, eventData.Message.Data.Period);
            _logger.Information(
                "NewKLineHandler: Consume KLineDto:klineGroupName:{klineGroupName},Period:{period},Timestamp:{timestamp}",
                klineGroupName, eventData.Message.Data.Period, eventData.Message.Data.Timestamp);
            await _hubContext.Clients.Group(klineGroupName).SendAsync("ReceiveKline", eventData.Message.Data);
        }
    }
}