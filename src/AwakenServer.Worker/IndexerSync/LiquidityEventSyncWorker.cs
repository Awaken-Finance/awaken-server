using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AwakenServer.Chains;
using AwakenServer.Common;
using AwakenServer.Provider;
using AwakenServer.Trade;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AwakenServer.Worker.IndexerSync;

/**
 * sync swap-indexer to awaken-server
 */
public class LiquidityEventSyncWorker : AwakenServerWorkerBase
{
    protected override WorkerBusinessType _businessType => WorkerBusinessType.LiquidityEvent;
 
    protected readonly IChainAppService _chainAppService;
    protected readonly IGraphQLProvider _graphQlProvider;
    private readonly ILiquidityAppService _liquidityService;

    public LiquidityEventSyncWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ILiquidityAppService liquidityService,
        IOptionsMonitor<WorkerOptions> optionsMonitor,
        IGraphQLProvider graphQlProvider,
        IChainAppService chainAppService,
        IOptions<ChainsInitOptions> chainsOption,
        ISyncStateProvider syncStateProvider)
        : base(timer, serviceScopeFactory, optionsMonitor, graphQlProvider, chainAppService, chainsOption, syncStateProvider)
    {
        _chainAppService = chainAppService;
        _graphQlProvider = graphQlProvider;
        _liquidityService = liquidityService;
    }

    public override async Task<long> SyncDataAsync(ChainDto chain, long startHeight)
    {
        var queryList = await _graphQlProvider.GetLiquidRecordsAsync(chain.Id, startHeight, 0, 0, _workerOptions.QueryOnceLimit);
        
        long blockHeight = -1;

        foreach (var queryDto in queryList)
        {
            if (!await _liquidityService.CreateAsync(queryDto))
            {
                continue;
            }

            blockHeight = Math.Max(blockHeight, queryDto.BlockHeight);
        }
       

        return blockHeight;
    }
    
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await DealDataAsync();
    }
}