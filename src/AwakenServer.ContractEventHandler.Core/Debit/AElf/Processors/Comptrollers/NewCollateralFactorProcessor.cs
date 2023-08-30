using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using Awaken.Contracts.Controller;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.Debits.Entities.Ef;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Debit.AElf.Processors.Comptrollers
{
    public class NewCollateralFactorProcessor : AElfEventProcessorBase<CollateralFactorChanged>
    {
        private readonly IRepository<CToken> _cTokenRepository;
        private readonly IChainAppService _chainAppService;
        private readonly ILogger<NewCollateralFactorProcessor> _logger;

        public NewCollateralFactorProcessor(IRepository<CToken> cTokenRepository,
            IChainAppService chainAppService, ILogger<NewCollateralFactorProcessor> logger)
        {
            _cTokenRepository = cTokenRepository;
            _chainAppService = chainAppService;
            _logger = logger;
        }

        protected override async Task HandleEventAsync(CollateralFactorChanged eventDetailsEto, EventContext txInfoDto)
        {
            _logger.LogInformation($"CollateralFactorChanged Trigger: {eventDetailsEto}");
            var chainId = txInfoDto.ChainId;
            var chain = await _chainAppService.GetByChainIdCacheAsync(chainId.ToString());
            var cToken =
                await _cTokenRepository.GetAsync(
                    x => x.ChainId == chain.Id && x.Address == eventDetailsEto.AToken.ToBase58());
            cToken.CollateralFactorMantissa = eventDetailsEto.NewCollateralFactor.ToString();
            await _cTokenRepository.UpdateAsync(cToken);
        }
    }
}