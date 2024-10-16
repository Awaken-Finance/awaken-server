using AwakenServer.Asset;
using AwakenServer.Chains;
using AwakenServer.Common;
using AwakenServer.Provider;
using AwakenServer.Trade.Index;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AwakenServer.Worker.DataCleanup;

public class DataCleanupWorker : AwakenServerWorkerBase
{
    protected override WorkerBusinessType _businessType => WorkerBusinessType.DataCleanup;

    private readonly IMyPortfolioAppService _portfolioAppService;
    private readonly DataCleanupWorkerSettings _options;

    public DataCleanupWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IOptionsMonitor<WorkerOptions> optionsMonitor,
        IOptionsMonitor<DataCleanupWorkerSettings> cleanupOptionsMonitor,
        IGraphQLProvider graphQlProvider,
        IChainAppService chainAppService,
        IOptions<ChainsInitOptions> chainsOption,
        IMyPortfolioAppService portfolioAppService,
        ISyncStateProvider syncStateProvider)
        : base(timer, serviceScopeFactory, optionsMonitor, graphQlProvider, chainAppService, chainsOption, syncStateProvider)
    {
        _portfolioAppService = portfolioAppService;
        _options = cleanupOptionsMonitor.CurrentValue;
        cleanupOptionsMonitor.OnChange((newOptions, _) =>
        {
            _options.ExecuteDeletion = newOptions.ExecuteDeletion;
            _options.DataVersion = newOptions.DataVersion;
            _options.Indexes = newOptions.Indexes;
            _logger.Information($"Data cleanup, options change: " +
                                   $"ExecuteDeletion={_options.ExecuteDeletion}, " +
                                   $"DataVersion={_options.DataVersion}, " +
                                   $"Indexes={_options.Indexes}");
        });
    }

    public override async Task<long> SyncDataAsync(ChainDto chain, long startHeight)
    {
        var userLiquidityIndexName = typeof(CurrentUserLiquidityIndex).Name.ToLower();
        var userLiquiditySnapshotIndexName = typeof(UserLiquiditySnapshotIndex).Name.ToLower();
        
        _logger.Information($"Data cleanup, begin with options: {JsonConvert.SerializeObject(_options)}");
        
        foreach (var indexName in _options.Indexes)
        {
            bool result = false;
            if (indexName == userLiquidityIndexName)
            {
                result = await _portfolioAppService.CleanupUserLiquidityDataAsync(_options.DataVersion, _options.ExecuteDeletion);
            }
            else if (indexName == userLiquiditySnapshotIndexName)
            {
                result = await _portfolioAppService.CleanupUserLiquiditySnapshotsDataAsync(_options.DataVersion, _options.ExecuteDeletion);
                
            }
            if (result)
            {
                _logger.Information($"Data cleanup, index: {indexName}, version: {_options.DataVersion} done");
            }
            else
            {
                _logger.Error($"Data cleanup, index: {indexName}, version: {_options.DataVersion} failed");
            }
        }

        return 0;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await DealDataAsync();
    }
}