using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Chains;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.EntityHandler.Services;

public class ChainInitializeService : ITransientDependency
{
    private readonly ChainsInitOptions _chainsOption;
    private readonly IChainAppService _chainAppService;
    private readonly IObjectMapper _objectMapper;
    private readonly INESTRepository<Chain, string> _chainIndexRepository;
    private readonly ILogger<ChainInitializeService> _logger;
    
    public ChainInitializeService(IOptions<ChainsInitOptions> chainsOption, IChainAppService chainAppService,
        IObjectMapper objectMapper, INESTRepository<Chain, string> chainIndexRepository, ILogger<ChainInitializeService> logger)
    {
        _chainsOption = chainsOption.Value;
        _chainAppService = chainAppService;
        _objectMapper = objectMapper;
        _chainIndexRepository = chainIndexRepository;
        _logger = logger;
    }
    
    public async Task InitializeDataAsync()
    {
        if (_chainsOption == null)
        {
            return;
        }
        
        foreach (var chainsOptionChain in _chainsOption.Chains)
        {
            var chainDto = await _chainAppService.GetByNameCacheAsync(chainsOptionChain.Name);
            if (chainDto != null)
            {
                continue;
            }
            
            Log.Information("Initialize chain {chainId}-{chainName}-{AElfChainId}",chainsOptionChain.Id,chainsOptionChain.Name,chainsOptionChain.AElfChainId);
            await _chainIndexRepository.AddOrUpdateAsync(_objectMapper.Map<ChainDto, Chain>(chainsOptionChain));
        }
    }
}