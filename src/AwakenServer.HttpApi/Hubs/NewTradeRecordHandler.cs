using System.Threading.Tasks;
using AwakenServer.Trade.Dtos;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AwakenServer.Hubs
{
    public class NewTradeRecordHandler : IConsumer<NewIndexEvent<TradeRecordIndexDto>>, ITransientDependency
    {
        private readonly IHubContext<TradeHub> _hubContext;
        private readonly ITradeHubConnectionProvider _tradeHubConnectionProvider;
        private readonly ITradeHubGroupProvider _tradeHubGroupProvider;
        private readonly ILogger<NewTradeRecordHandler> _logger;

        public NewTradeRecordHandler(ITradeHubConnectionProvider tradeHubConnectionProvider,
            IHubContext<TradeHub> hubContext, ITradeHubGroupProvider tradeHubGroupProvider,
            ILogger<NewTradeRecordHandler> logger)
        {
            _tradeHubConnectionProvider = tradeHubConnectionProvider;
            _hubContext = hubContext;
            _tradeHubGroupProvider = tradeHubGroupProvider;
            _logger = logger;
        }

        /*public async Task HandleEventAsync(NewIndexEvent<TradeRecordIndexDto> eventData)
        {
            var tradeRecordGroupName =
                _tradeHubGroupProvider.GetTradeRecordGroupName(eventData.Data.ChainId, eventData.Data.TradePair.Id, 0);
            _logger.LogInformation("NewTradeRecordHandler,ReceiveTradeRecord: {tradeRecordGroupName}", tradeRecordGroupName);
            await _hubContext.Clients.Group(tradeRecordGroupName).SendAsync("ReceiveTradeRecord", eventData.Data);

            var connectionId = _tradeHubConnectionProvider.GetUserConnection(eventData.Data.ChainId,
                eventData.Data.TradePair.Id, eventData.Data.Address, 0);
            if (string.IsNullOrEmpty(connectionId)) return;
            _logger.LogInformation("NewTradeRecordHandler,ReceiveUserTradeRecord: {connectionId}", connectionId);
            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveUserTradeRecord", eventData.Data);

        }*/

        public async Task Consume(ConsumeContext<NewIndexEvent<TradeRecordIndexDto>> eventData)
        {
            var tradeRecordGroupName =
                _tradeHubGroupProvider.GetTradeRecordGroupName(eventData.Message.Data.ChainId,
                    eventData.Message.Data.TradePair.Id, 0);
            _logger.LogInformation("NewTradeRecordHandler,ReceiveTradeRecord: {tradeRecordGroupName}",
                tradeRecordGroupName);
            await _hubContext.Clients.Group(tradeRecordGroupName)
                .SendAsync("ReceiveTradeRecord", eventData.Message.Data);

            var connectionIds = _tradeHubConnectionProvider.GetUserConnectionList(eventData.Message.Data.ChainId,
                eventData.Message.Data.TradePair.Id, eventData.Message.Data.Address);
            if (connectionIds == null) return;
            _logger.LogInformation("NewTradeRecordHandler,ReceiveUserTradeRecord: {connectionId}", connectionIds);
            foreach (var connectionId in connectionIds)
            {
                await _hubContext.Clients.Client(connectionId)
                    .SendAsync("ReceiveUserTradeRecord", eventData.Message.Data);
            }
        }
    }
}