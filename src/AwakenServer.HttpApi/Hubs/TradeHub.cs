using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AwakenServer.Activity;
using AwakenServer.Activity.Dtos;
using AwakenServer.Models;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Serilog;
using Volo.Abp.AspNetCore.SignalR;

namespace AwakenServer.Hubs
{
    public class TradeHub : AbpHub
    {
        private readonly ITradeRecordAppService _tradeRecordAppService;
        private readonly ITradeHubConnectionProvider _tradeHubConnectionProvider;
        private readonly ITradeHubGroupProvider _tradeHubGroupProvider;
        private readonly IKLineAppService _kLineAppService;
        private readonly ITradePairAppService _tradePairAppService;
        private readonly IActivityAppService _activityAppService;
        private readonly ILogger<TradeHub> _logger;

        public TradeHub(ITradeRecordAppService tradeRecordAppService,
            ITradeHubConnectionProvider tradeHubConnectionProvider,
            IKLineAppService kLineAppService, ITradeHubGroupProvider tradeHubGroupProvider,
            ITradePairAppService tradePairAppService,
            IActivityAppService activityAppService,
            ILogger<TradeHub> logger)
        {
            _tradeRecordAppService = tradeRecordAppService;
            _tradeHubConnectionProvider = tradeHubConnectionProvider;
            _kLineAppService = kLineAppService;
            _tradeHubGroupProvider = tradeHubGroupProvider;
            _tradePairAppService = tradePairAppService;
            _activityAppService = activityAppService;
            _logger = logger;
        }

        public async Task RequestTradeRecord(string chainId, string tradePairId, long timestamp, int maxResultCount)
        {
            var chain = chainId;
            var pairId = Guid.Parse(tradePairId);

            await Groups.AddToGroupAsync(Context.ConnectionId,
                _tradeHubGroupProvider.GetTradeRecordGroupName(chain, pairId, timestamp));

            var records = await _tradeRecordAppService.GetListWithSubRecordsAsync(new GetTradeRecordsInput
            {
                ChainId = chain,
                TradePairId = pairId,
                TimestampMin = timestamp,
                SkipCount = 0,
                MaxResultCount = maxResultCount
            });
            Log.Information("RequestTradeRecord TimestampMin: {timestamp}",
                timestamp == 0 ? DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.Date.AddHours(-8)) : timestamp);
            await Clients.Caller.SendAsync("ReceiveTradeRecords", new TradeRecordModel<List<TradeRecordIndexDto>>
            {
                ChainId = chain,
                TradePairId = pairId,
                Data = records.Items.ToList()
            });
        }

        public async Task UnsubscribeTradeRecord(string chainId, string tradePairId, long timestamp)
        {
            var chain = chainId;
            var pairId = Guid.Parse(tradePairId);
            await TryRemoveFromGroupAsync(Context.ConnectionId,
                _tradeHubGroupProvider.GetTradeRecordGroupName(chain, pairId, timestamp));
        }

        public async Task RequestRemovedTradeRecord(string chainId, string tradePairId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId,
                _tradeHubGroupProvider.GetRemovedTradeRecordGroupName(chainId, Guid.Parse(tradePairId)));
        }

        public async Task UnsubscribeRemovedTradeRecord(string chainId, string tradePairId)
        {
            await TryRemoveFromGroupAsync(Context.ConnectionId,
                _tradeHubGroupProvider.GetRemovedTradeRecordGroupName(chainId, Guid.Parse(tradePairId)));
        }

        public async Task RequestKline(string chainId, string tradePairId, int period, long from, long to)
        {
            var pairId = Guid.Parse(tradePairId);
            var timestampMin = from - from % (period * 1000);

            await Groups.AddToGroupAsync(Context.ConnectionId,
                _tradeHubGroupProvider.GetKlineGroupName(chainId, pairId, period));


            var kLines = await _kLineAppService.GetListAsync(new GetKLinesInput
            {
                ChainId = chainId,
                TradePairId = pairId,
                Period = period,
                TimestampMin = timestampMin,
                TimestampMax = to
            });

            await Clients.Caller.SendAsync("ReceiveKLines", new KLineModel<List<KLineDto>>
            {
                ChainId = chainId,
                TradePairId = pairId,
                Period = period,
                From = timestampMin,
                To = to,
                Data = kLines.Items.ToList()
            });
        }

        public async Task UnsubscribeKline(string chainId, string tradePairId, int period)
        {
            var pairId = Guid.Parse(tradePairId);
            await TryRemoveFromGroupAsync(Context.ConnectionId,
                _tradeHubGroupProvider.GetKlineGroupName(chainId, pairId, period));
        }

        public async Task RequestTradePair(string chainId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId,
                _tradeHubGroupProvider.GetTradePairGroupName(chainId));
        }

        public async Task UnsubscribeTradePair(string chainId)
        {
            await TryRemoveFromGroupAsync(Context.ConnectionId,
                _tradeHubGroupProvider.GetTradePairGroupName(chainId));
        }

        public async Task RequestTradePairDetail(string tradePairId)
        {
            var pairId = Guid.TryParse(tradePairId, out var tradePairGuid) ? tradePairGuid : Guid.Empty;
            if (pairId == Guid.Empty)
            {
                return;
            }

            var tradePairIndexDto = await _tradePairAppService.GetFromGrainAsync(pairId);
            if (tradePairIndexDto == null)
            {
                return;
            }

            await Clients.Caller.SendAsync("ReceiveTradePairDetail", tradePairIndexDto);

            await Groups.AddToGroupAsync(Context.ConnectionId,
                _tradeHubGroupProvider.GetTradePairDetailName(tradePairId));
        }

        public async Task UnsubscribeTradePairDetail(string tradePairId)
        {
            await TryRemoveFromGroupAsync(Context.ConnectionId,
                _tradeHubGroupProvider.GetTradePairDetailName(tradePairId));
        }

        public async Task RequestUserTradeRecord(string chainId, string tradePairId, string address, long timestamp,
            int maxResultCount)
        {
            var pairId = Guid.Parse(tradePairId);

            _tradeHubConnectionProvider.AddUserConnection(chainId, pairId, address, Context.ConnectionId);
            var records = await _tradeRecordAppService.GetListWithSubRecordsAsync(new GetTradeRecordsInput
            {
                ChainId = chainId,
                TradePairId = pairId,
                Address = address,
                TimestampMin = timestamp,
                SkipCount = 0,
                MaxResultCount = maxResultCount
            });

            await Clients.Caller.SendAsync("ReceiveUserTradeRecords",
                new UserTradeRecordModel<List<TradeRecordIndexDto>>
                {
                    ChainId = chainId,
                    TradePairId = pairId,
                    Address = address,
                    Data = records.Items.ToList()
                });
        }

        public async Task UnsubscribeUserTradeRecord(string chainId, string tradePairId, string address, long timestamp)
        {
            var pairId = Guid.Parse(tradePairId);
            _tradeHubConnectionProvider.ClearUserConnection(chainId, pairId, address, timestamp, Context.ConnectionId);
        }

        public async Task RequestRemovedUserTradeRecord(string chainId, string tradePairId, string address)
        {
            _tradeHubConnectionProvider.AddRemovedUserConnection(chainId, Guid.Parse(tradePairId), address,
                Context.ConnectionId);
        }

        public async Task UnsubscribeRemovedUserTradeRecord(string chainId, string tradePairId, string address)
        {
            _tradeHubConnectionProvider.ClearRemovedUserConnection(chainId, Guid.Parse(tradePairId), address,
                Context.ConnectionId);
        }
        
        public async Task RequestActivityRankingList(int activityId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId,
                _tradeHubGroupProvider.GetActivityRankingListGroupName(activityId));
            
            var rankingListDto = await _activityAppService.GetRankingListAsync(new ActivityBaseDto()
            {
                ActivityId = activityId
            });

            await Clients.Caller.SendAsync("ReceiveActivityRankingList", rankingListDto);
        }

        public async Task UnsubscribeActivityRankingList(int activityId)
        {
            await TryRemoveFromGroupAsync(Context.ConnectionId,
                _tradeHubGroupProvider.GetActivityRankingListGroupName(activityId));
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var tradeGroups = _tradeHubGroupProvider.GetAllTradeRecordGroup();
            foreach (var group in tradeGroups)
            {
                await TryRemoveFromGroupAsync(Context.ConnectionId, group);
            }

            var klineGroups = _tradeHubGroupProvider.GetAllKlineGroup();
            foreach (var group in klineGroups)
            {
                await TryRemoveFromGroupAsync(Context.ConnectionId, group);
            }

            var tradePairGroups = _tradeHubGroupProvider.GetAllTradePairGroup();
            foreach (var group in tradePairGroups)
            {
                await TryRemoveFromGroupAsync(Context.ConnectionId, group);
            }

            var activityRankingListGroups = _tradeHubGroupProvider.GetAllActivityRankingListGroup();
            foreach (var group in activityRankingListGroups)
            {
                await TryRemoveFromGroupAsync(Context.ConnectionId, group);
            }

            _tradeHubConnectionProvider.ClearUserConnection(Context.ConnectionId);
            _tradeHubConnectionProvider.ClearRemovedUserConnection(Context.ConnectionId);

            await base.OnDisconnectedAsync(exception);
        }
        
        [ExceptionHandler(typeof(Exception),
            Message = "RemoveFromGroup Error", TargetType = typeof(HandlerExceptionService), MethodName = nameof(HandlerExceptionService.HandleWithReturnBool))]
        protected virtual async Task<bool> TryRemoveFromGroupAsync(string connectionId, string groupName)
        {
            await Groups.RemoveFromGroupAsync(connectionId, groupName);
            return true;
        }
    }
}