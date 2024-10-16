using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwakenServer.Trade.Dtos;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AwakenServer.Hubs
{
    public class RemoveTradeRecordHandler : IConsumer<RemovedIndexEvent<TradeRecordRemovedListResultDto>>,
        ITransientDependency
    {
        private readonly IHubContext<TradeHub> _hubContext;
        private readonly ITradeHubConnectionProvider _tradeHubConnectionProvider;
        private readonly ITradeHubGroupProvider _tradeHubGroupProvider;
        private readonly ILogger _logger;

        public RemoveTradeRecordHandler(ITradeHubConnectionProvider tradeHubConnectionProvider,
            IHubContext<TradeHub> hubContext,
            ITradeHubGroupProvider tradeHubGroupProvider)
        {
            _tradeHubConnectionProvider = tradeHubConnectionProvider;
            _hubContext = hubContext;
            _tradeHubGroupProvider = tradeHubGroupProvider;
            _logger = Log.ForContext<RemoveTradeRecordHandler>();
        }

        public async Task Consume(ConsumeContext<RemovedIndexEvent<TradeRecordRemovedListResultDto>> eventData)
        {
            var dic = eventData.Message.Data.Items.ToLookup(t => $"{t.ChainId}_{t.TradePairId}").ToDictionary(
                t => t.Key,
                t => t.Select(t => new ReceiveTradeRecordRemovedDto { TransactionHash = t.TransactionHash }));
            foreach (var keyValuePair in dic)
            {
                var key = keyValuePair.Key.Split("_");
                if (key.Length < 2) continue;
                var tradeRecordGroupName =
                    _tradeHubGroupProvider.GetRemovedTradeRecordGroupName(key[0], Guid.Parse(key[1]));

                _logger.Information(
                    "RemoveTradeRecordHandler,ReceiveRemovedTradeRecord: {tradeRecordGroupName}, {count}",
                    tradeRecordGroupName, keyValuePair.Value.ToList().Count);
                await _hubContext.Clients.Group(tradeRecordGroupName)
                    .SendAsync("ReceiveRemovedTradeRecord", new RemovedIndexEvent<List<ReceiveTradeRecordRemovedDto>>
                    {
                        Data = keyValuePair.Value.ToList()
                    });
            }

            var userDic = eventData.Message.Data.Items.ToLookup(t => $"{t.ChainId}_{t.TradePairId}_{t.Address}")
                .ToDictionary(t => t.Key,
                    t => t.Select(t => new ReceiveTradeRecordRemovedDto { TransactionHash = t.TransactionHash }));
            foreach (var keyValuePair in userDic)
            {
                var key = keyValuePair.Key.Split("_");
                if (key.Length < 3) continue;
                var connectionIds =
                    _tradeHubConnectionProvider.GetRemovedUserConnectionList(key[0],
                        Guid.Parse(key[1]), key[2]);
                if (connectionIds == null) continue;

                _logger.Information(
                    "RemoveTradeRecordHandler,ReceiveRemovedUserTradeRecord: {connectionId}, {count}", connectionIds,
                    keyValuePair.Value.ToList().Count);

                foreach (var connectionId in connectionIds)
                {
                    await _hubContext.Clients.Client(connectionId)
                        .SendAsync("ReceiveRemovedUserTradeRecord",
                            new RemovedIndexEvent<List<ReceiveTradeRecordRemovedDto>>
                            {
                                Data = keyValuePair.Value.ToList()
                            });
                }
            }
        }
    }
}