using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.CMS;
using AwakenServer.Common;
using AwakenServer.Provider;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using DnsClient;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AwakenServer.Worker.IndexerSync;

public class TradePairPriceUpdateWorker : AwakenServerWorkerBase
{
    protected override WorkerBusinessType _businessType => WorkerBusinessType.TradePairPriceUpdate;
    
    protected readonly IChainAppService _chainAppService;
    protected readonly IGraphQLProvider _graphQlProvider;
    private readonly ITradePairAppService _tradePairAppService;
    
    public TradePairPriceUpdateWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ITradePairAppService tradePairAppService, ILogger<AwakenServerWorkerBase> logger,
        IOptionsMonitor<WorkerOptions> optionsMonitor,
        IGraphQLProvider graphQlProvider,
        IChainAppService chainAppService,
        IOptions<ChainsInitOptions> chainsOption)
        : base(timer, serviceScopeFactory, optionsMonitor, graphQlProvider, chainAppService, logger, chainsOption)
    {
        _chainAppService = chainAppService;
        _graphQlProvider = graphQlProvider;
        _tradePairAppService = tradePairAppService;
    }

    public override async Task<long> SyncDataAsync(ChainDto chain, long startHeight, long newIndexHeight)
    {
        Dictionary<string, List<SyncRecordDto>> pair2syncs = new Dictionary<string, List<SyncRecordDto>>();
        
        long blockHeight = -1;

        while (true)
        {
            var queryList = await _graphQlProvider.GetSyncRecordsAsync(chain.Id, blockHeight+1, 0, 0, _workerOptions.QueryOnceLimit);
            if (queryList.Count <= 0)
            {
                break;
            }
            _logger.LogInformation("sync queryList count: {count} ,chainId:{chainId}", queryList.Count, chain.Id);
        
            try
            {
                foreach (var queryDto in queryList)
                {
                    if (!pair2syncs.ContainsKey(queryDto.PairAddress))
                    {
                        pair2syncs[queryDto.PairAddress] = new List<SyncRecordDto>();
                    }
                    pair2syncs[queryDto.PairAddress].Add(queryDto);
                    blockHeight = Math.Max(blockHeight, queryDto.BlockHeight);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "sync event fail.");
            }

        }
        
        var affected = await _tradePairAppService.Update24hPriceAsync(pair2syncs);
        
        _logger.LogInformation($"align trade pair 24h price high and low end, affected trade pairs: {affected}");
        
        return blockHeight;
    }
    
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await DealDataAsync();
    }
}