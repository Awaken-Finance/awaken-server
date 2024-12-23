using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AwakenServer.Chains;
using AwakenServer.Common;
using AwakenServer.Provider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AwakenServer.Worker;

public abstract class AwakenServerWorkerBase : AsyncPeriodicBackgroundWorkerBase
{
    protected abstract WorkerBusinessType _businessType { get; }
    protected WorkerSetting _workerOptions { get; set; } = new();
    protected readonly ILogger _logger;
    protected readonly IChainAppService _chainAppService;
    protected readonly IGraphQLProvider _graphQlProvider;
    protected readonly ISyncStateProvider _syncStateProvider;
    protected readonly Dictionary<string, bool> _chainHasResetBlockHeight = new();

    protected AwakenServerWorkerBase(AbpAsyncTimer timer, 
        IServiceScopeFactory serviceScopeFactory, 
        IOptionsMonitor<WorkerOptions> optionsMonitor,
        IGraphQLProvider graphQlProvider,
        IChainAppService chainAppService,
        IOptions<ChainsInitOptions> chainsOption,
        ISyncStateProvider syncStateProvider) : base(timer, serviceScopeFactory)
    {
        _logger = Log.ForContext<AwakenServerWorkerBase>();
        _chainAppService = chainAppService;
        _graphQlProvider = graphQlProvider;
        _syncStateProvider = syncStateProvider;

        timer.Period = optionsMonitor.CurrentValue.GetWorkerSettings(_businessType).TimePeriod;
        
        _workerOptions.TimePeriod = optionsMonitor.CurrentValue.GetWorkerSettings(_businessType) != null ?
            optionsMonitor.CurrentValue.GetWorkerSettings(_businessType).TimePeriod : 3000;
        
        _workerOptions.OpenSwitch = optionsMonitor.CurrentValue.GetWorkerSettings(_businessType) != null ?
            optionsMonitor.CurrentValue.GetWorkerSettings(_businessType).OpenSwitch : false;
        
        _workerOptions.ResetBlockHeightFlag = optionsMonitor.CurrentValue.GetWorkerSettings(_businessType) != null ?
            optionsMonitor.CurrentValue.GetWorkerSettings(_businessType).ResetBlockHeightFlag : false;
        
        _workerOptions.ResetBlockHeight = optionsMonitor.CurrentValue.GetWorkerSettings(_businessType) != null ?
            optionsMonitor.CurrentValue.GetWorkerSettings(_businessType).ResetBlockHeight : 0;
        
        _workerOptions.QueryStartBlockHeightOffset = optionsMonitor.CurrentValue.GetWorkerSettings(_businessType) != null ?
            optionsMonitor.CurrentValue.GetWorkerSettings(_businessType).QueryStartBlockHeightOffset : -1;
        
        _workerOptions.QueryOnceLimit = optionsMonitor.CurrentValue.GetWorkerSettings(_businessType) != null ?
            optionsMonitor.CurrentValue.GetWorkerSettings(_businessType).QueryOnceLimit : 10000;
        
        _workerOptions.IsSyncHistoryData = optionsMonitor.CurrentValue.GetWorkerSettings(_businessType) != null ?
            optionsMonitor.CurrentValue.GetWorkerSettings(_businessType).IsSyncHistoryData : false;
        
        _workerOptions.DataVersion = optionsMonitor.CurrentValue.GetWorkerSettings(_businessType) != null ?
            optionsMonitor.CurrentValue.GetWorkerSettings(_businessType).DataVersion : "v1";
        
        _logger.Information($"AwakenServerWorkerBase: BusinessType: {_businessType.ToString()}," +
                               $"Start with config: " +
                               $"TimePeriod: {timer.Period}, " +
                               $"ResetBlockHeightFlag: {_workerOptions.ResetBlockHeightFlag}, " +
                               $"ResetBlockHeight:{_workerOptions.ResetBlockHeight}," +
                               $"OpenSwitch: {_workerOptions.OpenSwitch}," +
                               $"QueryStartBlockHeightOffset: {_workerOptions.QueryStartBlockHeightOffset}");

        foreach (var chain in chainsOption.Value.Chains)
        {
            _chainHasResetBlockHeight[chain.Name] = false;
        }
        
        //to change timer Period if the WorkerOptions has changed.
        optionsMonitor.OnChange((newOptions, _) =>
        {
            var workerSetting = newOptions.GetWorkerSettings(_businessType);
            
            timer.Period = workerSetting.TimePeriod;
            
            _workerOptions = workerSetting;
            
            if (_workerOptions.OpenSwitch)
            {
                timer.Start();
            }
            else
            {
                timer.Stop();
            }
            
            if (_workerOptions.ResetBlockHeightFlag)
            {
                foreach (var chainHasResetBlockHeight in _chainHasResetBlockHeight)
                {
                    _chainHasResetBlockHeight[chainHasResetBlockHeight.Key] = false;
                    _logger.Information($"On options change, chain: {chainHasResetBlockHeight.Key}, HasResetBlockHeight: {_chainHasResetBlockHeight[chainHasResetBlockHeight.Key]}");
                }
            }
            
            _logger.Information(
                $"The workerSetting of Worker {_businessType} has changed to {JsonConvert.SerializeObject(_workerOptions)}");
        });
    }
    
    public abstract Task<long> SyncDataAsync(ChainDto chain, long startHeight);

    [ExceptionHandler(typeof(Exception), Message = "ResetBlockHeight Error", TargetType = typeof(HandlerExceptionService), MethodName = nameof(HandlerExceptionService.HandleWithReturn))]
    public virtual async Task ResetBlockHeight(ChainDto chain)
    {
        AsyncHelper.RunSync(async () =>
            await _graphQlProvider.SetLastEndHeightAsync(chain.Name, _businessType,
                _workerOptions.ResetBlockHeight));
        _chainHasResetBlockHeight[chain.Name] = true;
        _logger.Information($"Reset block height. chain: {chain.Name}, type: {_businessType.ToString()}, block height: {_workerOptions.ResetBlockHeight}, chain has reset block height: {_chainHasResetBlockHeight[chain.Name]}");
    }
    
    
    public async Task DealDataAsync()
    {
        if (!_workerOptions.OpenSwitch)
        {
            return;
        }
        
        var chains = await _chainAppService.GetListAsync(new GetChainInput());
        if (_workerOptions.ResetBlockHeightFlag)
        {
            foreach (var chain in chains.Items)
            {
                if (!_chainHasResetBlockHeight[chain.Name])
                {
                    await ResetBlockHeight(chain);
                }
            }
        }
        
        foreach (var chain in chains.Items)
        {
            var lastEndHeight = await _graphQlProvider.GetLastEndHeightAsync(chain.Name, _businessType);

            if (lastEndHeight <= 0)
            {
                continue;
            }
            
            _logger.Information(
                $"Start deal data for businessType: {_businessType} " +
                $"chainId: {chain.Name}, " +
                $"lastEndHeight: {lastEndHeight}, " +
                $"ResetBlockHeightFlag: {_workerOptions.ResetBlockHeightFlag}, " +
                $"ResetBlockHeight: {_workerOptions.ResetBlockHeight}, " +
                $"QueryStartBlockHeightOffset: {_workerOptions.QueryStartBlockHeightOffset}, " +
                $"startHeight: {lastEndHeight + _workerOptions.QueryStartBlockHeightOffset}");

            var blockHeight = await SyncDataAsync(chain, lastEndHeight + _workerOptions.QueryStartBlockHeightOffset);

            if (blockHeight > 0)
            {
                if (_workerOptions.ResetBlockHeightFlag && !_chainHasResetBlockHeight[chain.Name])
                {
                    await ResetBlockHeight(chain);
                }
                else
                {
                    await _graphQlProvider.SetLastEndHeightAsync(chain.Name, _businessType, blockHeight);
                }
            }

            _logger.Information(
                "End deal data for businessType: {businessType} chainId: {chainId} blockHeight: {BlockHeight} lastEndHeight:{lastEndHeight}",
                _businessType, chain.Name, blockHeight, lastEndHeight);
        }
    }
}