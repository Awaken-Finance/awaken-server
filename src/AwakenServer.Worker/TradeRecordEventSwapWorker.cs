using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.Provider;
using AwakenServer.Trade;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AwakenServer.Worker;

public class TradeRecordEventSwapWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IChainAppService _chainAppService;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly ITradeRecordAppService _tradeRecordAppService;
    private readonly ILogger<TradeRecordEventSwapWorker> _logger;

    public TradeRecordEventSwapWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IChainAppService chainAppService, IGraphQLProvider iGraphQlProvider, 
        ITradeRecordAppService tradeRecordAppService, ILogger<TradeRecordEventSwapWorker> logger)
        : base(timer, serviceScopeFactory)
    {
        _graphQlProvider = iGraphQlProvider;
        _chainAppService = chainAppService;
        _tradeRecordAppService = tradeRecordAppService;
        _logger = logger;
        timer.Period = WorkerOptions.TimePeriod;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var chains = await _chainAppService.GetListAsync(new GetChainInput());
        foreach (var chain in chains.Items)
        {
            var lastEndHeight = await _graphQlProvider.GetLastEndHeightAsync(chain.Name, QueryType.TradeRecord);
            _logger.LogInformation("swap first lastEndHeight: {lastEndHeight}", lastEndHeight);

            if(lastEndHeight < 0) continue;
            var queryList = await _graphQlProvider.GetSwapRecordsAsync(chain.Name, lastEndHeight, 0);
            _logger.LogInformation("swap queryList count: {count}", queryList.Count);
            foreach (var queryDto in queryList)
            {
                if(!await _tradeRecordAppService.CreateAsync(queryDto)) continue;
                await _graphQlProvider.SetLastEndHeightAsync(chain.Name, QueryType.TradeRecord, queryDto.BlockHeight);
                _logger.LogInformation("swap success lastEndHeight: {BlockHeight}", queryDto.BlockHeight);
            }
        }
    }
}