using System.Collections.Generic;
using System.Linq;
using AwakenServer.Asset;
using AwakenServer.Chains;
using AwakenServer.Common;
using AwakenServer.Provider;
using AwakenServer.Trade.Index;
using System.Threading.Tasks;
using AwakenServer.Route;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AwakenServer.Worker.DataCleanup;

public class CacheDataMoveWorker : AwakenServerWorkerBase
{
    protected override WorkerBusinessType _businessType => WorkerBusinessType.CacheDataMove;
    private readonly IBestRoutesAppService _bestRoutesAppService;
    private readonly Dictionary<string, bool> _chainIsExecuted = new();
    
    public CacheDataMoveWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IOptionsMonitor<WorkerOptions> optionsMonitor,
        IOptionsMonitor<DataCleanupWorkerSettings> cleanupOptionsMonitor,
        IGraphQLProvider graphQlProvider,
        IChainAppService chainAppService,
        IOptions<ChainsInitOptions> chainsOption,
        IBestRoutesAppService portfolioAppService,
        ISyncStateProvider syncStateProvider)
        : base(timer, serviceScopeFactory, optionsMonitor, graphQlProvider, chainAppService, chainsOption, syncStateProvider)
    {
        _bestRoutesAppService = portfolioAppService;
    }

    public override async Task<long> SyncDataAsync(ChainDto chain, long startHeight)
    {
        if (!_chainIsExecuted.ContainsKey(chain.Name))
        {
            await _bestRoutesAppService.UpdateGrainIdsCacheKeyAsync(chain.Name);
            _chainIsExecuted[chain.Name] = true;
        }
        return 0;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await DealDataAsync();
    }
}