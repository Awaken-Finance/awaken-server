using System.Threading.Tasks;
using AwakenServer.Activity.Dtos;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.Hubs;

public class ActivityRankingListHandler : IConsumer<NewIndexEvent<RankingListDto>>, ITransientDependency
{
    private readonly IHubContext<TradeHub> _hubContext;
    private readonly ITradeHubGroupProvider _tradeHubGroupProvider;
    private readonly ILogger _logger;

    public ActivityRankingListHandler(IHubContext<TradeHub> hubContext, ITradeHubGroupProvider tradeHubGroupProvider)
    {
        _hubContext = hubContext;
        _tradeHubGroupProvider = tradeHubGroupProvider;
        _logger = Log.ForContext<ActivityRankingListHandler>();
    }

    public async Task Consume(ConsumeContext<NewIndexEvent<RankingListDto>> context)
    {
        var activityRankingListGroup =
            _tradeHubGroupProvider.GetActivityRankingListGroupName(context.Message.Data.ActivityId);
        _logger.Information(
            "ActivityRankingListHandler: Consume RankingListDto:activityRankingListGroup:ActivityId:{ActivityId}",
            context.Message.Data.ActivityId);
        await _hubContext.Clients.Group(activityRankingListGroup).SendAsync("ReceiveActivityRankingList", context.Message.Data);
    }
}