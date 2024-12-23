namespace AwakenServer.Worker.IndexerReSync;

using System;
using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.Common;
using AwakenServer.Provider;
using AwakenServer.StatInfo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

public class StatInfoEventReSyncWorker : AwakenServerWorkerBase
{
    protected override WorkerBusinessType _businessType => WorkerBusinessType.NewVersionStatInfoIndexEvent;
 
    private readonly IStatInfoInternalAppService _statInfoInternalAppService;

    public StatInfoEventReSyncWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory, 
        IOptionsMonitor<WorkerOptions> optionsMonitor, IGraphQLProvider graphQlProvider, IChainAppService chainAppService, 
        IOptions<ChainsInitOptions> chainsOption, ISyncStateProvider syncStateProvider, 
        StatInfoInternalAppService statInfoInternalAppService) : 
        base(timer, serviceScopeFactory, optionsMonitor, graphQlProvider, chainAppService, chainsOption, syncStateProvider)
    {
        _statInfoInternalAppService = statInfoInternalAppService;
    }

    public override async Task<long> SyncDataAsync(ChainDto chain, long startHeight)
    {
        var syncRecordList = await _graphQlProvider.GetSyncRecordsAsync(chain.Id, startHeight, 0, 0, _workerOptions.QueryOnceLimit);
        var maxBlockHeight = 0L;
        if (syncRecordList.Count >= _workerOptions.QueryOnceLimit)
        {
            maxBlockHeight = syncRecordList[_workerOptions.QueryOnceLimit - 1].BlockHeight;
        }
        var liquidityRecordList = await _graphQlProvider.GetLiquidRecordsAsync(chain.Id, startHeight, 
            maxBlockHeight, 0, _workerOptions.QueryOnceLimit);
        var swapRecordList = await _graphQlProvider.GetSwapRecordsAsync(chain.Id, startHeight, 
            maxBlockHeight, 0, _workerOptions.QueryOnceLimit);
        _logger.Information("StatInfoEventReSyncWorker: chain: {chainName}, liquidity queryList count: {liquidityCount}, swap queryList count: {swapCount}, sync queryList count: {syncCount}", 
            chain.Name, liquidityRecordList.Count, swapRecordList.Count, syncRecordList.Count);
        long blockHeight = -1;
        try
        {
            foreach (var liquidityRecord in liquidityRecordList)
            {
                await _statInfoInternalAppService.CreateLiquidityRecordAsync(liquidityRecord, _workerOptions.DataVersion);
            }
            foreach (var swapRecord in swapRecordList)
            {
                await _statInfoInternalAppService.CreateSwapRecordAsync(swapRecord, _workerOptions.DataVersion);
            }
            foreach (var syncRecord in syncRecordList)
            {
                await _statInfoInternalAppService.CreateSyncRecordAsync(syncRecord, _workerOptions.DataVersion);
                blockHeight =  Math.Max(blockHeight, syncRecord.BlockHeight);
            }
        }
        catch (Exception e)
        {
            _logger.Error(e, "New Version StatInfo event fail.");
        }

        return blockHeight;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await DealDataAsync();
    }
}