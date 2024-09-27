using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Activity.Eto;
using AwakenServer.Activity.Index;
using AwakenServer.EntityHandler.Trade;
using AwakenServer.Trade.Etos;
using AwakenServer.Trade.Index;
using Microsoft.Extensions.Logging;
using Volo.Abp.EventBus.Distributed;

namespace AwakenServer.EntityHandler.Activity;

public class RankingListSnapshotIndexHandler : TradeIndexHandlerBase, 
    IDistributedEventHandler<RankingListSnapshotEto>
{
    private readonly INESTRepository<RankingListSnapshotIndex, Guid> _rankingListSnapshotIndexRepository;
    private readonly ILogger<RankingListSnapshotIndexHandler> _logger;

    public RankingListSnapshotIndexHandler(INESTRepository<RankingListSnapshotIndex, Guid> rankingListSnapshotIndexRepository, ILogger<RankingListSnapshotIndexHandler> logger)
    {
        _rankingListSnapshotIndexRepository = rankingListSnapshotIndexRepository;
        _logger = logger;
    }

    public async Task HandleEventAsync(RankingListSnapshotEto eventData)
    {
        var rankingListSnapshotIndex = ObjectMapper.Map<RankingListSnapshotEto, RankingListSnapshotIndex>(eventData);
        var existedIndex = await _rankingListSnapshotIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.ActivityId).Value(eventData.ActivityId)) &&
            q.Term(i => i.Field(f => f.Timestamp).Value(eventData.Timestamp)));
        rankingListSnapshotIndex.Id = existedIndex switch
        {
            null => Guid.NewGuid(),
            _ => existedIndex.Id
        };
        await _rankingListSnapshotIndexRepository.AddOrUpdateAsync(rankingListSnapshotIndex);
    }
}