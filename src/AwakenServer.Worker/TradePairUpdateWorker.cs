using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AwakenServer.Worker
{
    public class TradePairUpdateWorker : AsyncPeriodicBackgroundWorkerBase
    {
        private readonly IChainAppService _chainAppService;
        private readonly ITradePairAppService _tradePairAppService;

        public TradePairUpdateWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
            ITradePairAppService tradePairAppService, IChainAppService chainAppService)
            : base(timer, serviceScopeFactory)
        {
            _tradePairAppService = tradePairAppService;
            _chainAppService = chainAppService;
            timer.Period = WorkerOptions.PairUpdatePeriod;
        }

        protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
        {
            var chains = await _chainAppService.GetListAsync(new GetChainInput());
            foreach (var chain in chains.Items)
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
            }
        }
    }
}
