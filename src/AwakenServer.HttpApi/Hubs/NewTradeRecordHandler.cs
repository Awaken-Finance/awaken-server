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


        public async Task Consume(ConsumeContext<NewIndexEvent<TradeRecordIndexDto>> eventData)
        {
            var tradeRecordGroupName =
                _tradeHubGroupProvider.GetTradeRecordGroupName(eventData.Message.Data.ChainId,
                    eventData.Message.Data.TradePair.Id, 0);
            Log.Information(
                "NewTradeRecordHandler,ReceiveTradeRecord: {tradeRecordGroupName},address: {address}",
                tradeRecordGroupName, eventData.Message.Data.Address);
            await _hubContext.Clients.Group(tradeRecordGroupName)
                .SendAsync("ReceiveTradeRecord", eventData.Message.Data);

            var connectionIds = _tradeHubConnectionProvider.GetUserConnectionList(eventData.Message.Data.ChainId,
                eventData.Message.Data.TradePair.Id, eventData.Message.Data.Address);
            if (connectionIds == null) return;
            Log.Information("NewTradeRecordHandler,ReceiveUserTradeRecord: {connectionId},address:{address}",
                connectionIds, eventData.Message.Data.Address);
            foreach (var connectionId in connectionIds)
            {
                await _hubContext.Clients.Client(connectionId)
                    .SendAsync("ReceiveUserTradeRecord", eventData.Message.Data);
            }
        }
    }
}