using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Tokens;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.EventBus.Local;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.ContractEventHandler.Tokens;

public class NewTokenHandler: IDistributedEventHandler<NewTokenEvent>,ITransientDependency
{
    private readonly INESTRepository<Token, Guid> _tokenIndexRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<NewTokenHandler> _logger;
    
    public NewTokenHandler(INESTRepository<Token, Guid> tokenIndexRepository,
        IObjectMapper objectMapper,
        ILogger<NewTokenHandler> logger)
    {
        _tokenIndexRepository = tokenIndexRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }
    
    public async Task HandleEventAsync(NewTokenEvent eventData)
    {
        try
        {
            await _tokenIndexRepository.AddOrUpdateAsync(_objectMapper.Map<NewTokenEvent, Token>(eventData));
            _logger.LogDebug("Token info add success:{Id}-{Symbol}-{ChainId}, ImageUri:{ImageUri}", eventData.Id, eventData.Symbol,
                eventData.ChainId, eventData.ImageUri);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Id}-{Symbol}-{ChainId}", eventData.Id, eventData.Symbol,
                eventData.ChainId);
        }
    }
    
}