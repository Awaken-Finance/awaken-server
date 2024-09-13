using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.Common;
using AwakenServer.Provider;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AwakenServer.Worker
{
    public class TradePairUpdateWorker : AwakenServerWorkerBase
    {
        protected override WorkerBusinessType _businessType => WorkerBusinessType.TradePairUpdate;
        
        protected readonly IChainAppService _chainAppService;
        protected readonly IGraphQLProvider _graphQlProvider;
        private readonly ITradePairAppService _tradePairAppService;
        
        public TradePairUpdateWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
            ITradePairAppService tradePairAppService, IChainAppService chainAppService,
            IGraphQLProvider graphQlProvider,
            IOptionsMonitor<WorkerOptions> optionsMonitor,
            ILogger<AwakenServerWorkerBase> logger,
            IOptions<ChainsInitOptions> chainsOption,
            ISyncStateProvider syncStateProvider)
            : base(timer, serviceScopeFactory, optionsMonitor, graphQlProvider, chainAppService, logger, chainsOption, syncStateProvider)
        {
            _chainAppService = chainAppService;
            _graphQlProvider = graphQlProvider;
            _tradePairAppService = tradePairAppService;
        }

        public override async Task<long> SyncDataAsync(ChainDto chain, long startHeight)
        {
            var pairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = chain.Name,
                MaxResultCount = 1000
            });
            foreach (var pair in pairs.Items)
            {
                await _tradePairAppService.UpdateTradePairAsync(pair.Id);
            }

            return 0;
        }
        
        protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
        {
            await DealDataAsync();
        }
    }
}
