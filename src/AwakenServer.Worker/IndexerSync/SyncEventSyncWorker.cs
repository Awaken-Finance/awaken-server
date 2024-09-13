using System;
using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.CMS;
using AwakenServer.Common;
using AwakenServer.Provider;
using AwakenServer.Trade;
using DnsClient;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AwakenServer.Worker.IndexerSync;

public class SyncEventSyncWorker : AwakenServerWorkerBase
{
    protected override WorkerBusinessType _businessType => WorkerBusinessType.SyncEvent;
    
    protected readonly IChainAppService _chainAppService;
    protected readonly IGraphQLProvider _graphQlProvider;
    private readonly ITradePairAppService _tradePairAppService;

    public SyncEventSyncWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
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
        
        var queryList = await _graphQlProvider.GetSyncRecordsAsync(chain.Id, startHeight, 0, 0, _workerOptions.QueryOnceLimit);
        
        _logger.LogInformation("sync queryList count: {count} ,chainId:{chainId}", queryList.Count, chain.Id);
        
        try
        {
            foreach (var queryDto in queryList)
            {
                await _tradePairAppService.CreateSyncAsync(queryDto);
                blockHeight = Math.Max(blockHeight, queryDto.BlockHeight);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "sync event fail.");
        }

        return blockHeight;
    }
    
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await DealDataAsync();
    }
}