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

        public UserLiquidityUpdateWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
            IMyPortfolioAppService myPortfolioAppService, IChainAppService chainAppService,
            IGraphQLProvider graphQlProvider,
            IOptionsMonitor<WorkerOptions> optionsMonitor,
            ILogger<AwakenServerWorkerBase> logger,
            IOptions<ChainsInitOptions> chainsOption,
            IOptionsSnapshot<PortfolioOptions> portfolioOptions,
            ISyncStateProvider syncStateProvider)
            : base(timer, serviceScopeFactory, optionsMonitor, graphQlProvider, chainAppService, logger, chainsOption, syncStateProvider)
        {
            _chainAppService = chainAppService;
            _graphQlProvider = graphQlProvider;
            _myPortfolioAppService = myPortfolioAppService;

        }

        public override async Task<long> SyncDataAsync(ChainDto chain, long startHeight)
        {
            var addresses = await _myPortfolioAppService.GetAllUserAddressesAsync(_workerOptions.DataVersion);
            foreach (var address in addresses)
            {
                try
                {
                    var count = await _myPortfolioAppService.UpdateUserAllAssetAsync(address, TimeSpan.FromMilliseconds(_workerOptions.TimePeriod), _workerOptions.DataVersion);
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
