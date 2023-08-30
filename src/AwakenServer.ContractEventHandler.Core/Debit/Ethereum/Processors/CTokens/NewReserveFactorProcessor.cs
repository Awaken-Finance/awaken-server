using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.BackgroundJob.Processors;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.CToken;
using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.Debits.Entities.Ef;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Debit.Ethereum.Processors.CTokens
{
    public class NewReserveFactorProcessor : EthereumEthereumEventProcessorBase<NewReserveFactor>
    {
        private readonly IChainAppService _chainAppService;
        private readonly IRepository<CToken> _cTokenRepository;

        public NewReserveFactorProcessor(IRepository<CToken> cTokenRepository,
            IChainAppService chainAppService)
        {
            _cTokenRepository = cTokenRepository;
            _chainAppService = chainAppService;
        }

        protected override async Task HandleEventAsync(NewReserveFactor eventDetailsEto,
            ContractEventDetailsDto contractEventDetailsDto)
        {
            if (contractEventDetailsDto.StatusEnum != ContractEventStatus.Confirmed)
            {
                return;
            }

            var nodeName = contractEventDetailsDto.NodeName;
            var chain = await _chainAppService.GetByNameCacheAsync(nodeName);
            var cToken = await _cTokenRepository.GetAsync(x =>
                x.ChainId == chain.Id && x.Address == contractEventDetailsDto.Address);

            cToken.ReserveFactorMantissa = eventDetailsEto.NewReserveFactorMantissa.ToString();
            await _cTokenRepository.UpdateAsync(cToken);
        }
    }
}