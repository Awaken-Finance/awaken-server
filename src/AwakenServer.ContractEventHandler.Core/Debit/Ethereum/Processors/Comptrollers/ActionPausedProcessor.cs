using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.BackgroundJob.Processors;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.Comptroller;
using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.Debits.Entities.Ef;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Debit.Ethereum.Processors.Comptrollers
{
    public class ActionPausedProcessor : EthereumEthereumEventProcessorBase<ActionPaused>
    {
        private readonly IChainAppService _chainAppService;
        private readonly IRepository<CToken> _cTokenRepository;

        public ActionPausedProcessor(IChainAppService chainAppService, IRepository<CToken> cTokenRepository)
        {
            _chainAppService = chainAppService;
            _cTokenRepository = cTokenRepository;
        }

        protected override async Task HandleEventAsync(ActionPaused eventDetailsEto,
            ContractEventDetailsDto contractEventDetailsDto)
        {
            if (contractEventDetailsDto.StatusEnum != ContractEventStatus.Confirmed)
            {
                return;
            }

            var nodeName = contractEventDetailsDto.NodeName;
            var chain = await _chainAppService.GetByNameCacheAsync(nodeName);
            var cToken =
                await _cTokenRepository.GetAsync(x => x.ChainId == chain.Id && x.Address == eventDetailsEto.CToken);
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