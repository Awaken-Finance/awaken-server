using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Asset;
using AwakenServer.Chains;
using AwakenServer.Common;
using AwakenServer.Provider;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;
using SwapRecord = AwakenServer.Trade.Dtos.SwapRecord;

namespace AwakenServer.Worker.IndexerSync;

/**
 * sync swap-indexer to awaken-server
 */
public class PortfolioEventSyncWorker : AwakenServerWorkerBase
{
    protected override WorkerBusinessType _businessType => WorkerBusinessType.PortfolioEvent;
 
    protected readonly IChainAppService _chainAppService;
    protected readonly IGraphQLProvider _graphQlProvider;
    private readonly IMyPortfolioAppService _portfolioAppService;

    public PortfolioEventSyncWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ILogger<AwakenServerWorkerBase> logger,
        IOptionsMonitor<WorkerOptions> optionsMonitor,
        IGraphQLProvider graphQlProvider,
        IChainAppService chainAppService,
        IOptions<ChainsInitOptions> chainsOption,
        IMyPortfolioAppService portfolioAppService)
        : base(timer, serviceScopeFactory, optionsMonitor, graphQlProvider, chainAppService, logger, chainsOption)
    {
        _chainAppService = chainAppService;
        _graphQlProvider = graphQlProvider;
        _portfolioAppService = portfolioAppService;
    }

    public override async Task<long> SyncDataAsync(ChainDto chain, long startHeight, long newIndexHeight)
    {
        var currentConfirmedHeight = await _graphQlProvider.GetIndexBlockHeightAsync(chain.Id);
        
        var swapRecordList = await _graphQlProvider.GetSwapRecordsAsync(chain.Id, startHeight, currentConfirmedHeight, 0, _workerOptions.QueryOnceLimit);
        var maxBlockHeight = currentConfirmedHeight;
        if (swapRecordList.Count >= _workerOptions.QueryOnceLimit)
        {
            maxBlockHeight = swapRecordList[_workerOptions.QueryOnceLimit - 1].BlockHeight;
        }
        var liquidityRecordList = await _graphQlProvider.GetLiquidRecordsAsync(chain.Id, startHeight, 
            maxBlockHeight, 0, _workerOptions.QueryOnceLimit);
        _logger.LogInformation("portfolioWorker: liquidity queryList count: {liquidityCount}, swap queryList count: {swapCount}", 
            liquidityRecordList.Count, swapRecordList.Count);
        long blockHeight = -1;
        var logCount = 0;
        try
        {
            for (;;)
            {
                GetEarliestRecord(liquidityRecordList, swapRecordList, out var liquidityRecord, out var swapRecord);
                if (liquidityRecord == null && swapRecord == null)
                {
                    break;
                }
                if (liquidityRecord != null)
                {
                    await _portfolioAppService.SyncLiquidityRecordAsync(liquidityRecord);
                    blockHeight = Math.Max(blockHeight, liquidityRecord.BlockHeight);
                    await Task.Delay(3000);
                }
                else
                {
                    await _portfolioAppService.SyncSwapRecordAsync(swapRecord);
                    blockHeight = Math.Max(blockHeight, swapRecord.BlockHeight);
                }

                if (logCount++ == 10)
                {
                    _logger.LogInformation("Portfolio blockHeight : {height}", blockHeight);
                    logCount = 0;
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Portfolio event fail.");
        }

        return blockHeight;
    }

    private void GetEarliestRecord(List<LiquidityRecordDto> liquidityList, List<SwapRecordDto> swapRecords, out LiquidityRecordDto liquidityRecord, out SwapRecordDto swapRecord)
    {
        liquidityRecord = null;
        swapRecord = null;
        if (liquidityList.Count == 0 && swapRecords.Count == 0)
        {
            return;
        }
        if (liquidityList.Count == 0)
        {
            swapRecord = swapRecords[0];
            swapRecords.RemoveAt(0);
            return;
        }
        if (swapRecords.Count == 0)
        {
            liquidityRecord = liquidityList[0];
            liquidityList.RemoveAt(0);
            return;
        }
        if (swapRecords[0].BlockHeight < liquidityList[0].BlockHeight)
        {
            swapRecord = swapRecords[0];
            swapRecords.RemoveAt(0);
        }
        else
        {
            liquidityRecord = liquidityList[0];
            liquidityList.RemoveAt(0);
        }
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await DealDataAsync();
    }
}