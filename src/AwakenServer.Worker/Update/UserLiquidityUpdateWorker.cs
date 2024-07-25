using System;
using System.Threading.Tasks;
using AwakenServer.Asset;
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
    public class UserLiquidityUpdateWorker : AwakenServerWorkerBase
    {
        protected override WorkerBusinessType _businessType => WorkerBusinessType.UserLiquidityUpdate;
        
        protected readonly IChainAppService _chainAppService;
        protected readonly IGraphQLProvider _graphQlProvider;
        
        private readonly IMyPortfolioAppService _myPortfolioAppService;
        private readonly IOptionsSnapshot<PortfolioOptions> _portfolioOptions;

        public UserLiquidityUpdateWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
            IMyPortfolioAppService myPortfolioAppService, IChainAppService chainAppService,
            IGraphQLProvider graphQlProvider,
            IOptionsMonitor<WorkerOptions> optionsMonitor,
            ILogger<AwakenServerWorkerBase> logger,
            IOptions<ChainsInitOptions> chainsOption,
            IOptionsSnapshot<PortfolioOptions> portfolioOptions)
            : base(timer, serviceScopeFactory, optionsMonitor, graphQlProvider, chainAppService, logger, chainsOption)
        {
            _chainAppService = chainAppService;
            _graphQlProvider = graphQlProvider;
            _myPortfolioAppService = myPortfolioAppService;
            _portfolioOptions = portfolioOptions;

        }

        public override async Task<long> SyncDataAsync(ChainDto chain, long startHeight, long newIndexHeight)
        {
            var addresses = await _myPortfolioAppService.GetAllUserAddressesAsync(_portfolioOptions.Value.DataVersion);
            foreach (var address in addresses)
            {
                try
                {
                    var count = await _myPortfolioAppService.UpdateUserAllAssetAsync(address, TimeSpan.FromMilliseconds(_workerOptions.TimePeriod), _portfolioOptions.Value.DataVersion);
                    _logger.LogInformation($"update user all liquidity address: {address}, affected liquidity count: {count}");
                }
                catch (Exception e)
                {
                    _logger.LogError($"update user all liquidity faild. address: {address}, {e}");
                }
                
            }
            return 0;
        }
        
        protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
        {
            await DealDataAsync();
        }
    }
}
