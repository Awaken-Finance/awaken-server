using System;
using System.Collections.Generic;
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

namespace AwakenServer.Worker;

public class TradeRecordEventSwapWorker : AwakenServerWorkerBase
{
    protected override WorkerBusinessType _businessType => WorkerBusinessType.SwapEvent;
    
    protected readonly IChainAppService _chainAppService;
    protected readonly IGraphQLProvider _graphQlProvider;
    private readonly ITradeRecordAppService _tradeRecordAppService;


    public TradeRecordEventSwapWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ITradeRecordAppService tradeRecordAppService, ILogger<AwakenServerWorkerBase> logger,
        IOptionsMonitor<WorkerOptions> optionsMonitor,
        IGraphQLProvider graphQlProvider,
        IChainAppService chainAppService,
        IOptions<ChainsInitOptions> chainsOption)
        : base(timer, serviceScopeFactory, optionsMonitor, graphQlProvider, chainAppService, logger, chainsOption)
    {
        _chainAppService = chainAppService;
        _graphQlProvider = graphQlProvider;
        _tradeRecordAppService = tradeRecordAppService;
    }

    public override async Task<long> SyncDataAsync(ChainDto chain, long startHeight, long newIndexHeight)
    {
        long blockHeight = -1;
        
        var currentConfirmedHeight = await _graphQlProvider.GetIndexBlockHeightAsync(chain.Id);
        var queryList = await _graphQlProvider.GetSwapRecordsAsync(chain.Id, startHeight, 0, 0, _workerOptions.QueryOnceLimit);
        
        _logger.LogInformation("swap queryList count: {count}", queryList.Count);
            
        foreach (var queryDto in queryList)
        {
            if (!await _tradeRecordAppService.CreateAsync(currentConfirmedHeight, queryDto))
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