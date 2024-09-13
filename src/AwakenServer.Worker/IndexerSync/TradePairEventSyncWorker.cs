using System;
using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.CMS;
using AwakenServer.Common;
using AwakenServer.Provider;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using DnsClient;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AwakenServer.Worker.IndexerSync;

public class TradePairEventSyncWorker : AwakenServerWorkerBase
{
    protected override WorkerBusinessType _businessType => WorkerBusinessType.TradePairEvent;
    
    protected readonly IChainAppService _chainAppService;
    protected readonly IGraphQLProvider _graphQlProvider;
    private readonly ITradePairAppService _tradePairAppService;
    
    public TradePairEventSyncWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ITradePairAppService tradePairAppService, ILogger<AwakenServerWorkerBase> logger,
        IOptionsMonitor<WorkerOptions> optionsMonitor,
        IGraphQLProvider graphQlProvider,
        IChainAppService chainAppService,
        IOptions<ChainsInitOptions> chainsOption,
        ISyncStateProvider syncStateProvider)
        : base(timer, serviceScopeFactory, optionsMonitor, graphQlProvider, chainAppService, logger, chainsOption, syncStateProvider)
    {
        _chainAppService = chainAppService;
        _graphQlProvider = graphQlProvider;
        _tradePairAppService = tradePairAppService;
    }

    public override async Task<long> SyncDataAsync(ChainDto chain, long startHeight)
    {
        long blockHeight = -1;
        
        var result = await _graphQlProvider.GetTradePairInfoListAsync(new GetTradePairsInfoInput
        {
            ChainId = chain.Id,
            StartBlockHeight = startHeight,
            EndBlockHeight = 0,
            SkipCount = 0,
            MaxResultCount = _workerOptions.QueryOnceLimit
        });

        foreach (var pair in result.TradePairInfoDtoList.Data)
        {
            blockHeight = Math.Max(blockHeight, pair.BlockHeight);

            _logger.LogInformation("Syncing {pairId}/{pairAddress} on {chainName}, {Token0Symbol}/{Token1Symbol}",
                pair.Id, pair.Address, chain, pair.Token0Symbol, pair.Token1Symbol);

            var token0 = await _tradePairAppService.SyncTokenAsync(pair.ChainId, pair.Token0Symbol, chain);
            var token1 = await _tradePairAppService.SyncTokenAsync(pair.ChainId, pair.Token1Symbol, chain);
            pair.Token0Id = token0.Id;
            pair.Token1Id = token1.Id;
            if (await _tradePairAppService.SyncPairAsync(pair, chain))
            {
                _logger.LogInformation("Syncing {pairId}/{pairAddress} on {chainName}, {Token0Symbol}/{Token1Symbol} done",
                    pair.Id, pair.Address, chain, pair.Token0Symbol, pair.Token1Symbol);
            }
            else
            {
                _logger.LogError("Syncing {pairId}/{pairAddress} on {chainName}, {Token0Symbol}/{Token1Symbol} failed",
                    pair.Id, pair.Address, chain, pair.Token0Symbol, pair.Token1Symbol);
            }
        }

        return blockHeight;
    }
    
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await DealDataAsync();
    }
}