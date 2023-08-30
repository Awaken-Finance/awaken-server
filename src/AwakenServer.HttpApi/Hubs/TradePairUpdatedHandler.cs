using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Trade.Dtos;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AwakenServer.Hubs
{
    public class TradePairUpdatedHandler : IConsumer<NewIndexEvent<TradePairIndexDto>>, ITransientDependency
    {
        private readonly IHubContext<TradeHub> _hubContext;
        private readonly ITradeHubGroupProvider _tradeHubGroupProvider;
        private readonly ILogger<TradePairUpdatedHandler> _logger;


        public TradePairUpdatedHandler(IHubContext<TradeHub> hubContext, ITradeHubGroupProvider tradeHubGroupProvider,
            ILogger<TradePairUpdatedHandler> logger)
        {
            _hubContext = hubContext;
            _tradeHubGroupProvider = tradeHubGroupProvider;
            _logger = logger;
        }

        /*public async Task HandleEventAsync(NewIndexEvent<TradePairIndexDto> eventData)
        {
            var tradePairGroupName =
                _tradeHubGroupProvider.GetTradePairGroupName(eventData.Data.ChainId);
            
            await _hubContext.Clients.Group(tradePairGroupName).SendAsync("ReceiveTradePair", eventData.Data);
            
            tradePairGroupName =
                _tradeHubGroupProvider.GetTradePairDetailName(eventData.Data.Id.ToString());
            /*_logger.LogInformation(
                "ReceiveTradePairDetail,tradePairId:{tradePairId},groupName:{gruop}", eventData.Data.Id,
                tradePairGroupName);#1#

            await _hubContext.Clients.Group(tradePairGroupName).SendAsync("ReceiveTradePairDetail", eventData.Data);
        }*/
        
        public async Task Consume(ConsumeContext<NewIndexEvent<TradePairIndexDto>> eventData)
        {
            var tradePairGroupName =
                _tradeHubGroupProvider.GetTradePairGroupName(eventData.Message.Data.ChainId);
            
            await _hubContext.Clients.Group(tradePairGroupName).SendAsync("ReceiveTradePair", eventData.Message.Data);
            
            tradePairGroupName =
                _tradeHubGroupProvider.GetTradePairDetailName(eventData.Message.Data.Id.ToString());
            /*_logger.LogInformation(
                "ReceiveTradePairDetail,tradePairId:{tradePairId},groupName:{gruop}", eventData.Data.Id,
                tradePairGroupName);*/

            await _hubContext.Clients.Group(tradePairGroupName).SendAsync("ReceiveTradePairDetail", eventData.Message.Data);
        }
    }
}