using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Chains;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.ContractEventHandler.Chains;

public class NewChainHandler : IDistributedEventHandler<NewChainEvent>, ITransientDependency
{
    private readonly INESTRepository<Chain, string> _chainIndexRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<NewChainHandler> _logger;
    
    public NewChainHandler(INESTRepository<Chain, string> chainIndexRepository,
        IObjectMapper objectMapper,
        ILogger<NewChainHandler> logger)
    {
        _chainIndexRepository = chainIndexRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }
    
    public async Task HandleEventAsync(NewChainEvent eventData)
    {
        try
        {
            await _chainIndexRepository.AddOrUpdateAsync(_objectMapper.Map<NewChainEvent, Chain>(eventData));
            _logger.LogDebug("Chain info add success: {Id}-{Name}-{AElfChainId}", eventData.Id, 
                eventData.Name, eventData.AElfChainId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chain info add Fail: {Id}-{Name}-{AElfChainId}", eventData.Id,
                eventData.Name, eventData.AElfChainId);
        }
    }
}