using System;
using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.Provider;
using AwakenServer.Trade;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AwakenServer.Worker;

public class TradePairEventSyncWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IChainAppService _chainAppService;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly ITradePairAppService _tradePairAppService;
    private readonly ILogger<TradePairEventSyncWorker> _logger;

    public TradePairEventSyncWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IChainAppService chainAppService, IGraphQLProvider iGraphQlProvider, 
        ITradePairAppService tradePairAppService, ILogger<TradePairEventSyncWorker> logger)
        : base(timer, serviceScopeFactory)
    {
        _graphQlProvider = iGraphQlProvider;
        _chainAppService = chainAppService;
        _tradePairAppService = tradePairAppService;
        _logger = logger;
        timer.Period = WorkerOptions.TimePeriod;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var chains = await _chainAppService.GetListAsync(new GetChainInput());
        foreach (var chain in chains.Items)
        {
            var lastEndHeight = await _graphQlProvider.GetLastEndHeightAsync(chain.Name, QueryType.Sync);
            var newIndexHeight = await _graphQlProvider.GetIndexBlockHeightAsync(chain.Name);
            _logger.LogInformation("sync lastEndHeight: {lastEndHeight}, newIndexHeight: {newIndexHeight}", lastEndHeight, newIndexHeight);
            if (lastEndHeight >= newIndexHeight)
            {
                continue;
            }

            var queryList = await _graphQlProvider.GetSyncRecordsAsync(chain.Name, lastEndHeight, 0);
            _logger.LogInformation("sync queryList count: {count}", queryList.Count);
            try
            {
                long blockHeight = -1;
                foreach (var queryDto in queryList)
                {
                    await _tradePairAppService.UpdateLiquidityAsync(queryDto);
                    blockHeight = Math.Max(blockHeight, queryDto.BlockHeight);
                }
                if (blockHeight > 0)
                {
                    await _graphQlProvider.SetLastEndHeightAsync(chain.Name, QueryType.Sync, blockHeight);
                    _logger.LogInformation("sync lastEndHeight: {BlockHeight}", blockHeight);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "sync event fail.");
            }
            
        }
    }
}