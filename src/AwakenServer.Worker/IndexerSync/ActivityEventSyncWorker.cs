using System;
using System.Threading.Tasks;
using AwakenServer.Activity;
using AwakenServer.Chains;
using AwakenServer.Common;
using AwakenServer.Provider;
using AwakenServer.Trade;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
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
    private readonly ActivityOptions _activityOptions;

    private const string VolumeActivityType = "volume";
    private const string TvlActivityType = "tvl";
    private const int TvlSnapshotTimeFactor = 3;

    public ActivityEventSyncWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ITradeRecordAppService tradeRecordAppService, ILogger<AwakenServerWorkerBase> logger,
        IOptionsMonitor<WorkerOptions> optionsMonitor,
        IGraphQLProvider graphQlProvider,
        IOptionsSnapshot<ActivityOptions> activityOptions,
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
        _activityOptions = activityOptions.Value;
    }

    private void SetNextExecutionTime(DateTime lastExecuteTime)
    {
        _nextLpSnapshotExecutionTime = RandomSnapshotHelper.GetNextLpSnapshotExecutionTime(_random, lastExecuteTime);
    }
    
    public override async Task<long> SyncDataAsync(ChainDto chain, long startHeight)
    {
        var now = DateTime.UtcNow;
        var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(now);
        // 1. Lp Snapshot: random execute
        if (_firstExecution)
        {
            _firstExecution = false; 
            SetNextExecutionTime(now);
            Log.Information($"Init LP snapshot worker, Next executing time set to: {_nextLpSnapshotExecutionTime}");
        }
        else
        {
            // activity begin
            foreach (var activity in _activityOptions.ActivityList)
            {
                if (activity.Type == TvlActivityType)
                {
                    var isActivityBeginTime = timestamp >= activity.BeginTime &&
                                      timestamp < activity.BeginTime + _workerOptions.TimePeriod * TvlSnapshotTimeFactor;
                    // Log.Information($"current: {timestamp}, begin time: {activity.BeginTime} - {activity.BeginTime + _workerOptions.TimePeriod * TvlSnapshotTimeFactor}, isActivityBeginTime: {isActivityBeginTime}");
                    if (isActivityBeginTime)
                    {
                        var success = await _activityAppService.CreateLpSnapshotAsync(timestamp, "worker activity begin");
                        if (success)
                        {
                            Log.Information($"Executing LP snapshot at activity begin done at: {timestamp}, activityId: {activity.ActivityId}, type: {activity.Type}");
                        }
                        else
                        {
                            Log.Error($"Executing LP snapshot at activity begin failed at: {timestamp}, activityId: {activity.ActivityId}, type: {activity.Type}");
                        }
                    }
                }
            }
            
            if (now.TimeOfDay >= _nextLpSnapshotExecutionTime && now.TimeOfDay < _nextLpSnapshotExecutionTime.Add(TimeSpan.FromSeconds(_workerOptions.TimePeriod * TvlSnapshotTimeFactor / 1000)))
            {
                SetNextExecutionTime(now);
                var success = await _activityAppService.CreateLpSnapshotAsync(timestamp, "worker");
                if (success)
                {
                    Log.Information($"Executing LP snapshot done at: {timestamp}, next executing time set to: {_nextLpSnapshotExecutionTime}");
                }
                else
                {
                    Log.Error($"Executing LP snapshot at activity failed at: {timestamp}, next executing time set to: {_nextLpSnapshotExecutionTime}");
                }

            }
        }
        
        // 2. Swap Value
        long blockHeight = -1;
        var swapRecordList = await _graphQlProvider.GetSwapRecordsAsync(chain.Id, startHeight, 0, 0, _workerOptions.QueryOnceLimit);
        var limitOrderFillRecordList = await _graphQlProvider.GetLimitOrderFillRecordsAsync(chain.Id, startHeight, 0, 0, _workerOptions.QueryOnceLimit);
        
        Log.Information($"Activity swap queryList count: {swapRecordList.Count}, limit fill record count: {limitOrderFillRecordList.Count}");

        foreach (var limitOrderFillRecord in limitOrderFillRecordList)
        {
            await _activityAppService.CreateLimitOrderFillRecordAsync(limitOrderFillRecord);
        }
        
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