using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;
using System;
using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.Common;
using AwakenServer.Provider;
using Microsoft.Extensions.Logging;


namespace AwakenServer.Worker;

public abstract class AwakenServerWorkerBase : AsyncPeriodicBackgroundWorkerBase
{
    protected abstract WorkerBusinessType _businessType { get; }

    protected WorkerSetting _workerOptions { get; set; } = new();
    
    protected readonly ILogger<AwakenServerWorkerBase> _logger;
    
    protected readonly IChainAppService _chainAppService;
    protected readonly IGraphQLProvider _graphQlProvider;
    

    protected AwakenServerWorkerBase(AbpAsyncTimer timer, 
        IServiceScopeFactory serviceScopeFactory, 
        IOptionsMonitor<WorkerOptions> optionsMonitor,
        IGraphQLProvider graphQlProvider,
        IChainAppService chainAppService,
        ILogger<AwakenServerWorkerBase> logger,
        IOptions<ChainsInitOptions> chainsOption) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _chainAppService = chainAppService;
        _graphQlProvider = graphQlProvider;
        
        timer.Period = optionsMonitor.CurrentValue.GetWorkerSettings(_businessType).TimePeriod;
        
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
        
        _logger.LogInformation($"AwakenServerWorkerBase: BusinessType: {_businessType.ToString()}," +
                               $"Start with config: " +
                               $"TimePeriod: {timer.Period}, " +
                               $"ResetBlockHeightFlag: {_workerOptions.ResetBlockHeightFlag}, " +
                               $"ResetBlockHeight:{_workerOptions.ResetBlockHeight}," +
                               $"OpenSwitch: {_workerOptions.OpenSwitch}," +
                               $"QueryStartBlockHeightOffset: {_workerOptions.QueryStartBlockHeightOffset}");

        foreach (var chain in chainsOption.Value.Chains)
        {
            if (_workerOptions.ResetBlockHeightFlag)
            {
                AsyncHelper.RunSync(async () =>
                    await _graphQlProvider.SetLastEndHeightAsync(chain.Name, _businessType,
                        _workerOptions.ResetBlockHeight));
                _logger.LogInformation($"Reset block height. chain: {chain.Name}, type: {_businessType.ToString()}, block height: {_workerOptions.ResetBlockHeight}");
            }
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
            
            foreach (var chain in chainsOption.Value.Chains)
            {
                if (_workerOptions.ResetBlockHeightFlag)
                {
                    AsyncHelper.RunSync(async () =>
                        await _graphQlProvider.SetLastEndHeightAsync(chain.Name, _businessType,
                            _workerOptions.ResetBlockHeight));
                    _logger.LogInformation($"Reset block height. chain: {chain.Name}, type: {_businessType.ToString()}, block height: {_workerOptions.ResetBlockHeight}");
                }
            }

            _logger.LogInformation(
                "The workerSetting of Worker {BusinessType} has changed to Period = {Period} ms, OpenSwitch = {OpenSwitch}.",
                _businessType, timer.Period, workerSetting.OpenSwitch);
        });
    }
    
    public abstract Task<long> SyncDataAsync(ChainDto chain, long startHeight, long newIndexHeight);
    
    public async Task DealDataAsync()
    {
        if (!_workerOptions.OpenSwitch)
        {
            return;
        }
        
        var chains = await _chainAppService.GetListAsync(new GetChainInput());
        foreach (var chain in chains.Items)
        {
            try
            {
                var lastEndHeight = await _graphQlProvider.GetLastEndHeightAsync(chain.Name, _businessType);
                var newIndexHeight = await _graphQlProvider.GetIndexBlockHeightAsync(chain.Name);
                
                _logger.LogInformation(
                    $"Start deal data for businessType: {_businessType} " +
                    $"chainId: {chain.Name}, " +
                    $"lastEndHeight: {lastEndHeight}, " +
                    $"newIndexHeight: {newIndexHeight}, " +
                    $"ResetBlockHeightFlag: {_workerOptions.ResetBlockHeightFlag}, " +
                    $"ResetBlockHeight: {_workerOptions.ResetBlockHeight}, " +
                    $"QueryStartBlockHeightOffset: {_workerOptions.QueryStartBlockHeightOffset}");
                
                var blockHeight = await SyncDataAsync(chain, lastEndHeight + _workerOptions.QueryStartBlockHeightOffset, newIndexHeight);

                if (blockHeight > 0)
                {
                    await _graphQlProvider.SetLastEndHeightAsync(chain.Name, _businessType, blockHeight);
                }
                
                _logger.LogInformation(
                    "End deal data for businessType: {businessType} chainId: {chainId} lastEndHeight: {BlockHeight}",
                    _businessType, chain.Name, blockHeight);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "DealDataAsync error businessType:{businessType} chainId: {chainId}",
                    _businessType.ToString(), chain.Name);
            }
        }
    }
}