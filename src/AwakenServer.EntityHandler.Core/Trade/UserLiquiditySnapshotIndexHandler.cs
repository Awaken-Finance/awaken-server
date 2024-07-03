using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Trade.Etos;
using AwakenServer.Trade.Index;
using Microsoft.Extensions.Logging;
using Volo.Abp.EventBus.Distributed;

namespace AwakenServer.EntityHandler.Trade;

public class UserLiquiditySnapshotIndexHandler : TradeIndexHandlerBase,
    IDistributedEventHandler<UserLiquiditySnapshotEto>
{
    private readonly INESTRepository<UserLiquiditySnapshotIndex, Guid> _currentUserLiquidityIndexRepository;
    private readonly ILogger<KLineIndexHandler> _logger;

    public UserLiquiditySnapshotIndexHandler(INESTRepository<UserLiquiditySnapshotIndex, Guid> currentUserLiquidityIndexRepository, ILogger<KLineIndexHandler> logger)
    {
        _currentUserLiquidityIndexRepository = currentUserLiquidityIndexRepository;
        _logger = logger;
    }

    public async Task HandleEventAsync(UserLiquiditySnapshotEto eventData)
    {
        var snapshotIndex = ObjectMapper.Map<UserLiquiditySnapshotEto, UserLiquiditySnapshotIndex>(eventData);
        var existedIndex = await _currentUserLiquidityIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.TradePairId).Value(eventData.TradePairId)) &&
            q.Term(i => i.Field(f => f.Address).Value(eventData.Address)) &&
            q.Term(i => i.Field(f => f.Version).Value(eventData.Version)) &&
            q.Term(i => i.Field(f => f.SnapShotTime).Value(eventData.SnapShotTime)));
        snapshotIndex.Id = existedIndex switch
        {
            null => Guid.NewGuid(),
            _ => existedIndex.Id
        };
        await _currentUserLiquidityIndexRepository.AddOrUpdateAsync(snapshotIndex);
    }
}