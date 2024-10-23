using System;
using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.Common;
using AwakenServer.Provider;
using AwakenServer.Trade;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AwakenServer.Worker.IndexerReSync;

public class SwapEventReSyncWorker : AwakenServerWorkerBase
{
    protected override WorkerBusinessType _businessType => WorkerBusinessType.NewSwapEvent;
    
    protected readonly IChainAppService _chainAppService;
    protected readonly IGraphQLProvider _graphQlProvider;
    private readonly ITradeRecordAppService _tradeRecordAppService;


    public SwapEventReSyncWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ITradeRecordAppService tradeRecordAppService,
        IOptionsMonitor<WorkerOptions> optionsMonitor,
        IGraphQLProvider graphQlProvider,
        IChainAppService chainAppService,
        IOptions<ChainsInitOptions> chainsOption,
        ISyncStateProvider syncStateProvider)
        : base(timer, serviceScopeFactory, optionsMonitor, graphQlProvider, chainAppService, chainsOption, syncStateProvider)
    {
        _chainAppService = chainAppService;
        _graphQlProvider = graphQlProvider;
        _tradeRecordAppService = tradeRecordAppService;
    }

    public override async Task<long> SyncDataAsync(ChainDto chain, long startHeight)
    {
        long blockHeight = -1;
        
        var queryList = await _graphQlProvider.GetSwapRecordsAsync(chain.Id, startHeight, 0, 0, _workerOptions.QueryOnceLimit);
        
        foreach (var queryDto in queryList)
        {
            if (!await _tradeRecordAppService.FillKLineIndexAsync(queryDto))
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