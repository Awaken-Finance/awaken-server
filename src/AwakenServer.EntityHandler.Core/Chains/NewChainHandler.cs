using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Chains;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
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
    
    [ExceptionHandler(typeof(Exception), Message = "Handle NewChainEvent Error", LogLevel = LogLevel.Error, 
        TargetType = typeof(HandlerExceptionService), MethodName = nameof(HandlerExceptionService.HandleWithReturn))]
    public virtual async Task HandleEventAsync(NewChainEvent eventData)
    {
        await _chainIndexRepository.AddOrUpdateAsync(_objectMapper.Map<NewChainEvent, Chain>(eventData));
            Log.Debug("Chain info add success: {Id}-{Name}-{AElfChainId}", eventData.Id, 
                eventData.Name, eventData.AElfChainId);
    }
}