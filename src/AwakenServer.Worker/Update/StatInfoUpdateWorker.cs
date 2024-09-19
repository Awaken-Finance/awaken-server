using System;
using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.Common;
using AwakenServer.DataInfo;
using AwakenServer.Provider;
using AwakenServer.StatInfo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AwakenServer.Worker;

public class StatInfoUpdateWorker : AwakenServerWorkerBase
{
    protected override WorkerBusinessType _businessType => WorkerBusinessType.StatInfoUpdateEvent;
 
    protected readonly IGraphQLProvider _graphQlProvider;
    private readonly IStatInfoInternalAppService _statInfoInternalAppService;
    private readonly StatInfoUpdateWorkerSettings _options;

    public StatInfoUpdateWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory, 
        IOptionsMonitor<WorkerOptions> optionsMonitor, IOptionsMonitor<StatInfoUpdateWorkerSettings> updateOptionsMonitor, 
        IGraphQLProvider graphQlProvider, IChainAppService chainAppService, 
        ILogger<AwakenServerWorkerBase> logger, IOptions<ChainsInitOptions> chainsOption, ISyncStateProvider syncStateProvider, 
        StatInfoInternalAppService statInfoInternalAppService) : 
        base(timer, serviceScopeFactory, optionsMonitor, graphQlProvider, chainAppService, logger, chainsOption, syncStateProvider)
    {
        _graphQlProvider = graphQlProvider;
        _statInfoInternalAppService = statInfoInternalAppService;
        _options = updateOptionsMonitor.CurrentValue;
        updateOptionsMonitor.OnChange((newOptions, _) =>
        {
            _options.DataVersion = newOptions.DataVersion;
            _options.ExecuteRefreshTvl = newOptions.ExecuteRefreshTvl;
            _logger.LogInformation($"Data cleanup, options change: " +
                                   $"DataVersion={_options.DataVersion}, " +
                                   $"ExecuteRefreshTvl={_options.ExecuteRefreshTvl}");
        });
    }

    public override async Task<long> SyncDataAsync(ChainDto chain, long startHeight)
    {
        await _statInfoInternalAppService.UpdateTokenFollowPairAsync(chain.Name, _options.DataVersion);
        if (_options.ExecuteRefreshTvl)
        {
            await _statInfoInternalAppService.RefreshTvlAsync(chain.Name, _options.DataVersion);
        }

        await _statInfoInternalAppService.ClearOldTransactionHistoryAsync(chain.Name, _options.DataVersion);
        return 0;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await DealDataAsync();
    }
}