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
    public class CompBorrowSpeedUpdatedProcessor : AElfEventProcessorBase<PlatformTokenSpeedUpdated>
    {
        private readonly IChainAppService _chainAppService;
        private readonly IRepository<CToken> _cTokenRepository;
        private readonly ILogger<CompBorrowSpeedUpdatedProcessor> _logger;

        public CompBorrowSpeedUpdatedProcessor(IChainAppService chainAppService,
            IRepository<CToken> cTokenRepository, ILogger<CompBorrowSpeedUpdatedProcessor> logger)
        {
            _chainAppService = chainAppService;
            _cTokenRepository = cTokenRepository;
            _logger = logger;
        }

        protected override async Task HandleEventAsync(PlatformTokenSpeedUpdated eventDetailsEto,
            EventContext txInfoDto)
        {
            _logger.LogInformation($"PlatformTokenSpeedUpdated Trigger: {eventDetailsEto}");
            var chainId = txInfoDto.ChainId;
            var chain = await _chainAppService.GetByChainIdCacheAsync(chainId.ToString());
            var cToken =
                await _cTokenRepository.GetAsync(x =>
                    x.ChainId == chain.Id && x.Address == eventDetailsEto.AToken.ToBase58());
            cToken.BorrowCompSpeed = eventDetailsEto.NewSpeed.ToString();
            cToken.SupplyCompSpeed = eventDetailsEto.NewSpeed.ToString();
            await _cTokenRepository.UpdateAsync(cToken);
        }
    }
}