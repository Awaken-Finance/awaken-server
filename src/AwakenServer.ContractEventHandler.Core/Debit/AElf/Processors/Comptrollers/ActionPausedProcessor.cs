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
    public class ActionPausedProcessor : AElfEventProcessorBase<ActionPaused>
    {
        private readonly IChainAppService _chainAppService;
        private readonly IRepository<CToken> _cTokenRepository;
        private readonly ILogger<ActionPausedProcessor> _logger;

        public ActionPausedProcessor(IChainAppService chainAppService, IRepository<CToken> cTokenRepository,
            ILogger<ActionPausedProcessor> logger)
        {
            _chainAppService = chainAppService;
            _cTokenRepository = cTokenRepository;
            _logger = logger;
        }

        protected override async Task HandleEventAsync(ActionPaused eventDetailsEto, EventContext txInfoDto)
        {
            _logger.LogInformation($"ActionPaused Trigger: {eventDetailsEto}");
            var chainId = txInfoDto.ChainId;
            var chain = await _chainAppService.GetByChainIdCacheAsync(chainId.ToString());
            var cToken =
                await _cTokenRepository.GetAsync(x =>
                    x.ChainId == chain.Id && x.Address == eventDetailsEto.AToken.ToBase58());
            switch (eventDetailsEto.Action)
            {
                case "Mint":
                    cToken.IsMintPaused = eventDetailsEto.PauseState;
                    break;
                case "Borrow":
                    cToken.IsBorrowPaused = eventDetailsEto.PauseState;
                    break;
            }

            await _cTokenRepository.UpdateAsync(cToken);
        }
    }
}