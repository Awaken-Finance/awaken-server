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
    public class TransactionRevertWorker : AwakenServerWorkerBase
    {
        protected override WorkerBusinessType _businessType => WorkerBusinessType.TransactionRevert;
        
        protected readonly IChainAppService _chainAppService;
        protected readonly IGraphQLProvider _graphQlProvider;
        private readonly ITradePairAppService _tradePairAppService;
        private readonly ILiquidityAppService _liquidityService;
        private readonly ITradeRecordAppService _tradeRecordAppService;

        
        public TransactionRevertWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
            ITradePairAppService tradePairAppService, IChainAppService chainAppService,
            IGraphQLProvider graphQlProvider,
            IOptionsMonitor<WorkerOptions> optionsMonitor,
            ILogger<AwakenServerWorkerBase> logger,
            ILiquidityAppService liquidityService,
            ITradeRecordAppService tradeRecordAppService,
            IOptions<ChainsInitOptions> chainsOption)
            : base(timer, serviceScopeFactory, optionsMonitor, graphQlProvider, chainAppService, logger, chainsOption)
        {
            _chainAppService = chainAppService;
            _graphQlProvider = graphQlProvider;
            _tradePairAppService = tradePairAppService;
            _liquidityService = liquidityService;
            _tradeRecordAppService = tradeRecordAppService;
        }

        public override async Task<long> SyncDataAsync(ChainDto chain, long startHeight, long newIndexHeight)
        {
            await _tradeRecordAppService.RevertTradeRecordAsync(chain.Id);
            await _liquidityService.RevertLiquidityAsync(chain.Id);
            await _tradePairAppService.RevertTradePairAsync(chain.Id);
            return 0;
        }
        
        protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
        {
            await DealDataAsync();
        }
    }
}
