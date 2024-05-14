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
    public class TradeRecordUpdateWorker : AwakenServerWorkerBase
    {
        protected override WorkerBusinessType _businessType => WorkerBusinessType.TradeRecordUpdate;
        
        protected readonly IChainAppService _chainAppService;
        protected readonly IGraphQLProvider _graphQlProvider;
        private readonly ITradeRecordAppService _tradeRecordAppService;
        
        public TradeRecordUpdateWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
            ITradeRecordAppService tradeRecordAppService, IChainAppService chainAppService,
            IGraphQLProvider graphQlProvider,
            IOptionsMonitor<WorkerOptions> optionsMonitor,
            ILogger<AwakenServerWorkerBase> logger,
            IOptions<ChainsInitOptions> chainsOption)
            : base(timer, serviceScopeFactory, optionsMonitor, graphQlProvider, chainAppService, logger, chainsOption)
        {
            _chainAppService = chainAppService;
            _graphQlProvider = graphQlProvider;
            _tradeRecordAppService = tradeRecordAppService;
        }

        public override async Task<long> SyncDataAsync(ChainDto chain, long startHeight, long newIndexHeight)
        {
            await _tradeRecordAppService.UpdateAllTxnFeeAsync(chain.Name);
            // await _tradeRecordAppService.RemoveDuplicatesAsync(chain.Name);
            return 0;
        }
        
        protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
        {
            await DealDataAsync();
        }
    }
}
