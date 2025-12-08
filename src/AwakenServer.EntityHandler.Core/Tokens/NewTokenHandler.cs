using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Tokens;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.EventBus.Local;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.ContractEventHandler.Tokens;

public class NewTokenHandler: IDistributedEventHandler<NewTokenEvent>,ITransientDependency
{
    private readonly INESTRepository<TokenEntity, Guid> _tokenIndexRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<NewTokenHandler> _logger;
    
    public NewTokenHandler(INESTRepository<TokenEntity, Guid> tokenIndexRepository,
        IObjectMapper objectMapper,
        ILogger<NewTokenHandler> logger)
    {
        _tokenIndexRepository = tokenIndexRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }
    
    [ExceptionHandler(typeof(Exception), LogOnly = true)]
    public virtual async Task HandleEventAsync(NewTokenEvent eventData)
    {
        await _tokenIndexRepository.AddOrUpdateAsync(_objectMapper.Map<NewTokenEvent, TokenEntity>(eventData));
            Log.Debug("Token info add success:{Id}-{Symbol}-{ChainId}, ImageUri:{ImageUri}", eventData.Id, eventData.Symbol,
                eventData.ChainId, eventData.ImageUri);
    }
    
}