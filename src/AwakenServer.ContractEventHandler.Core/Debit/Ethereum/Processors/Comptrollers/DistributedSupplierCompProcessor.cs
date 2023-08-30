using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.BackgroundJob.Processors;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.Comptroller;
using AwakenServer.ContractEventHandler.Helpers;
using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.Debits.Entities.Ef;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Debit.Ethereum.Processors.Comptrollers
{
    public class DistributedSupplierCompProcessor : EthereumEthereumEventProcessorBase<DistributedSupplierComp>
    {
        private readonly IChainAppService _chainAppService;
        private readonly IRepository<CToken> _cTokenRepository;
        private readonly IRepository<CTokenUserInfo> _userInfoRepository;

        public DistributedSupplierCompProcessor(IChainAppService chainAppService,
            IRepository<CToken> cTokenRepository, IRepository<CTokenUserInfo> userInfoRepository)
        {
            _chainAppService = chainAppService;
            _cTokenRepository = cTokenRepository;
            _userInfoRepository = userInfoRepository;
        }

        protected override async Task HandleEventAsync(DistributedSupplierComp eventDetailsEto,
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
            cToken.AccumulativeSupplyComp =
                CalculationHelper.Add(cToken.AccumulativeSupplyComp, eventDetailsEto.CompDelta);
            await _cTokenRepository.UpdateAsync(cToken);
            var user = await _userInfoRepository.GetAsync(x =>
                x.CTokenId == cToken.Id && x.User == eventDetailsEto.Supplier);
            user.AccumulativeSupplyComp =
                CalculationHelper.Add(user.AccumulativeSupplyComp, eventDetailsEto.CompDelta);
            await _userInfoRepository.UpdateAsync(user);
        }
    }
}