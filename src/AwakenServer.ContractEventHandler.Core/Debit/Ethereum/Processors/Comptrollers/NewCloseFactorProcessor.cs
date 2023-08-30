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
    public class NewCloseFactorProcessor : EthereumEthereumEventProcessorBase<NewCloseFactor>
    {
        private readonly IRepository<CompController> _compControllerRepository;
        private readonly IChainAppService _chainAppService;

        public NewCloseFactorProcessor(IRepository<CompController> compControllerRepository,
            IChainAppService chainAppService)
        {
            _compControllerRepository = compControllerRepository;
            _chainAppService = chainAppService;
        }

        protected override async Task HandleEventAsync(NewCloseFactor eventDetailsEto,
            ContractEventDetailsDto contractEventDetailsDto)
        {
            if (contractEventDetailsDto.StatusEnum != ContractEventStatus.Confirmed)
            {
                return;
            }

            var nodeName = contractEventDetailsDto.NodeName;
            var chain = await _chainAppService.GetByNameCacheAsync(nodeName);
            var compController = await _compControllerRepository.GetAsync(x => x.ChainId == chain.Id);
            compController.CloseFactorMantissa = eventDetailsEto.NewCloseFactorMantissa.ToString();
            await _compControllerRepository.UpdateAsync(compController);
        }
    }
}