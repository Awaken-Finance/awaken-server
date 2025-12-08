using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Trade.Etos;
using AwakenServer.Trade.Index;
using Microsoft.Extensions.Logging;
using Volo.Abp.EventBus.Distributed;

namespace AwakenServer.EntityHandler.Trade;

public class CurrentUserLiquidityIndexHandler : TradeIndexHandlerBase, 
    IDistributedEventHandler<CurrentUserLiquidityEto>
{
    private readonly INESTRepository<CurrentUserLiquidityIndex, Guid> _currentUserLiquidityIndexRepository;
    private readonly ILogger<CurrentUserLiquidityIndexHandler> _logger;

    public CurrentUserLiquidityIndexHandler(INESTRepository<CurrentUserLiquidityIndex, Guid> currentUserLiquidityIndexRepository, ILogger<CurrentUserLiquidityIndexHandler> logger)
    {
        _currentUserLiquidityIndexRepository = currentUserLiquidityIndexRepository;
        _logger = logger;
    }

    public async Task HandleEventAsync(CurrentUserLiquidityEto eventData)
    {
        var userLiquidityIndex = ObjectMapper.Map<CurrentUserLiquidityEto, CurrentUserLiquidityIndex>(eventData);
        var existedIndex = await _currentUserLiquidityIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.TradePairId).Value(eventData.TradePairId)) &&
            q.Term(i => i.Field(f => f.Version).Value(eventData.Version)) &&
            q.Term(i => i.Field(f => f.Address).Value(eventData.Address)));
        userLiquidityIndex.Id = existedIndex switch
        {
            null => Guid.NewGuid(),
            _ => existedIndex.Id
        };
        await _currentUserLiquidityIndexRepository.AddOrUpdateAsync(userLiquidityIndex);
    }
}