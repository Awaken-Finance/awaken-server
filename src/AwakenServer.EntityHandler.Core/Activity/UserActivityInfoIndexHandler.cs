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

public class UserActivityInfoIndexHandler : TradeIndexHandlerBase, 
    IDistributedEventHandler<UserActivityInfoEto>
{
    private readonly INESTRepository<UserActivityInfoIndex, Guid> _userActivityInfoIndexRepository;
    private readonly ILogger<UserActivityInfoIndexHandler> _logger;

    public UserActivityInfoIndexHandler(INESTRepository<UserActivityInfoIndex, Guid> userActivityInfoIndexRepository, ILogger<UserActivityInfoIndexHandler> logger)
    {
        _userActivityInfoIndexRepository = userActivityInfoIndexRepository;
        _logger = logger;
    }

    public async Task HandleEventAsync(UserActivityInfoEto eventData)
    {
        var userLiquidityIndex = ObjectMapper.Map<UserActivityInfoEto, UserActivityInfoIndex>(eventData);
        await _userActivityInfoIndexRepository.AddOrUpdateAsync(userLiquidityIndex);
    }
}