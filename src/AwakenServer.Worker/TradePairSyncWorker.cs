using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.Provider;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AwakenServer.Worker;

public class TradePairSyncWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IChainAppService _chainAppService;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly ILogger<TradePairSyncWorker> _logger;
    private readonly ITradePairAppService _tradePairAppService;

    public TradePairSyncWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IGraphQLProvider iGraphQlProvider, IChainAppService chainAppService,
        ITradePairAppService tradePairAppService, ILogger<TradePairSyncWorker> logger)
        : base(timer, serviceScopeFactory)
    {
        _graphQlProvider = iGraphQlProvider;
        _chainAppService = chainAppService;
        _logger = logger;
        _tradePairAppService = tradePairAppService;
        timer.Period = WorkerOptions.TimePeriod;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var chains = await _chainAppService.GetListAsync(new GetChainInput());
        foreach (var chain in chains.Items)
        {
            var result = await _graphQlProvider.GetTradePairInfoListAsync(new GetTradePairsInfoInput
            {
                ChainId = chain.Name
            });
            foreach (var pair in result.GetTradePairInfoList.Data)
            {
                _logger.LogInformation("Syncing {pairId} on {chainName}, {Token0Symbol}/{Token1Symbol}",
                    pair.Id, chain.Name, pair.Token0Symbol, pair.Token1Symbol);
                await _tradePairAppService.SyncTokenAsync(pair, chain);
                await _tradePairAppService.SyncPairAsync(pair, chain);
                //this is important, beacuse of the es sync time 
                await Task.Delay(5000);
            }
        }
    }
}