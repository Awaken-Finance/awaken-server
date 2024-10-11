using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Activity;
using AwakenServer.Activity.Dtos;
using AwakenServer.Activity.Eto;
using AwakenServer.Activity.Index;
using AwakenServer.EntityHandler.Trade;
using AwakenServer.Trade.Etos;
using AwakenServer.Trade.Index;
using MassTransit;
using Microsoft.Extensions.Logging;
using Nest;
using Volo.Abp.EventBus.Distributed;

namespace AwakenServer.EntityHandler.Activity;

public class RankingListSnapshotIndexHandler : TradeIndexHandlerBase, 
    IDistributedEventHandler<RankingListSnapshotEto>
{
    private readonly INESTRepository<RankingListSnapshotIndex, Guid> _rankingListSnapshotIndexRepository;
    private readonly ILogger<RankingListSnapshotIndexHandler> _logger;
    private readonly IActivityAppService _activityAppService;
    private readonly IBus _bus;

    public RankingListSnapshotIndexHandler(INESTRepository<RankingListSnapshotIndex, Guid> rankingListSnapshotIndexRepository,
        IBus ibus,
        IActivityAppService activityAppService,
        ILogger<RankingListSnapshotIndexHandler> logger)
    {
        _rankingListSnapshotIndexRepository = rankingListSnapshotIndexRepository;
        _bus = ibus;
        _activityAppService = activityAppService;
        _logger = logger;
    }

    public async Task HandleEventAsync(RankingListSnapshotEto eventData)
    {
        var rankingListSnapshotIndex = ObjectMapper.Map<RankingListSnapshotEto, RankingListSnapshotIndex>(eventData);
        await _rankingListSnapshotIndexRepository.AddOrUpdateAsync(rankingListSnapshotIndex);
        PublishRankingListAsync(eventData);
    }

    private async Task PublishRankingListAsync(RankingListSnapshotEto eventData)
    {
        await Task.Delay(3000);
        var rankingListDto = await _activityAppService.GetRankingListAsync(new ActivityBaseDto()
        {
            ActivityId = eventData.ActivityId
        });
        await _bus.Publish(new NewIndexEvent<RankingListDto>()
        {
            Data = rankingListDto
        });
    }
}