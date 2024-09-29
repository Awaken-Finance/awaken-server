using System;
using System.Threading.Tasks;
using AElf.CSharp.Core;
using AwakenServer.Activity;
using AwakenServer.Chains;
using AwakenServer.Common;
using AwakenServer.Provider;
using AwakenServer.Trade;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Caching;
using Volo.Abp.Threading;

namespace AwakenServer.Worker;

public class ActivityEventSyncWorker : AwakenServerWorkerBase
{
    protected override WorkerBusinessType _businessType => WorkerBusinessType.ActivityEvent;
    
    protected readonly IChainAppService _chainAppService;
    protected readonly IGraphQLProvider _graphQlProvider;
    private readonly ITradeRecordAppService _tradeRecordAppService;
    private readonly IActivityAppService _activityAppService;
    private readonly Random _random = new Random();
    private TimeSpan _nextLpSnapshotExecutionTime;
    private bool _firstExecution = true;


    public ActivityEventSyncWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ITradeRecordAppService tradeRecordAppService, ILogger<AwakenServerWorkerBase> logger,
        IOptionsMonitor<WorkerOptions> optionsMonitor,
        IGraphQLProvider graphQlProvider,
        IChainAppService chainAppService,
        IOptions<ChainsInitOptions> chainsOption,
        ISyncStateProvider syncStateProvider,
        IActivityAppService activityAppService)
        : base(timer, serviceScopeFactory, optionsMonitor, graphQlProvider, chainAppService, logger, chainsOption, syncStateProvider)
    {
        _chainAppService = chainAppService;
        _graphQlProvider = graphQlProvider;
        _tradeRecordAppService = tradeRecordAppService;
        _activityAppService = activityAppService;
    }

    private void SetNextExecutionTime(DateTime lastExecuteTime)
    {
        _nextLpSnapshotExecutionTime = RandomSnapshotHelper.GetNextLpSnapshotExecutionTime(_random, lastExecuteTime);
        _logger.LogInformation($"Executing LP snapshot task at: {lastExecuteTime}, next executing time set to: {_nextLpSnapshotExecutionTime}");
    }
    
    public override async Task<long> SyncDataAsync(ChainDto chain, long startHeight)
    {
        var now = DateTime.Now;
        // 1. Lp Snapshot: random execute
        if (_firstExecution)
        {
            _firstExecution = false; 
            SetNextExecutionTime(now);
            var success = await _activityAppService.CreateLpSnapshotAsync(DateTimeHelper.ToUnixTimeMilliseconds(now));
            if (success)
            {
                _logger.LogInformation($"Create Lp Snapshot done at: {now}");
            }
        }
        else
        {
            if (now.TimeOfDay >= _nextLpSnapshotExecutionTime && now.TimeOfDay < _nextLpSnapshotExecutionTime.Add(TimeSpan.FromSeconds(_workerOptions.TimePeriod / 1000)))
            {
                SetNextExecutionTime(now);
                var success = await _activityAppService.CreateLpSnapshotAsync(DateTimeHelper.ToUnixTimeMilliseconds(now));
                if (success)
                {
                    _logger.LogInformation($"Create Lp Snapshot done at: {now}");
                }
            }
        }
        
        // 2. Swap Value
        long blockHeight = -1;
        
        var swapRecordList = await _graphQlProvider.GetSwapRecordsAsync(chain.Id, startHeight, 0, 0, _workerOptions.QueryOnceLimit);
        
        _logger.LogInformation("Activity swap queryList count: {count}", swapRecordList.Count);
            
        foreach (var swapRecordDto in swapRecordList)
        {
            if (!await _activityAppService.CreateSwapAsync(swapRecordDto))
            {
                continue;
            }
            blockHeight = Math.Max(blockHeight, swapRecordDto.BlockHeight);
        }

        return blockHeight;
    }
    
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await DealDataAsync();
    }
    
}