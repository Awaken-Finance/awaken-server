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
        await _currentUserLiquidityIndexRepository.AddOrUpdateAsync(snapshotIndex);
    }
}