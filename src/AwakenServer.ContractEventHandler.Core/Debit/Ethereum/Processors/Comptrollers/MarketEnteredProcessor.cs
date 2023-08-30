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
    public class MarketEnteredProcessor : EthereumEthereumEventProcessorBase<MarketEntered>
    {
        private readonly IRepository<CTokenUserInfo> _userInfoRepository;
        private readonly IChainAppService _chainAppService;
        private readonly ICachedDataProvider<CToken> _cTokenProvider;

        public MarketEnteredProcessor(IRepository<CTokenUserInfo> userInfoRepository,
            IChainAppService chainAppService, ICachedDataProvider<CToken> cTokenProvider)
        {
            _userInfoRepository = userInfoRepository;
            _chainAppService = chainAppService;
            _cTokenProvider = cTokenProvider;
        }

        protected override async Task HandleEventAsync(MarketEntered eventDetailsEto,
            ContractEventDetailsDto contractEventDetailsDto)
        {
            if (contractEventDetailsDto.StatusEnum != ContractEventStatus.Confirmed)
            {
                return;
            }

            var nodeName = contractEventDetailsDto.NodeName;
            var chain = await _chainAppService.GetByNameCacheAsync(nodeName);
            var cToken = await _cTokenProvider.GetOrSetCachedDataAsync(chain.Id + eventDetailsEto.CToken,
                x => x.ChainId == chain.Id && x.Address == eventDetailsEto.CToken);
            var targetUser = await _userInfoRepository.FirstOrDefaultAsync(x =>
                x.ChainId == chain.Id && x.User == eventDetailsEto.Account &&
                x.CTokenId== cToken.Id);
            if (targetUser != null)
            {
                targetUser.IsEnteredMarket = true;
                await _userInfoRepository.UpdateAsync(targetUser);
                return;
            }
            
            await _userInfoRepository.InsertAsync(new CTokenUserInfo
            {
                User = eventDetailsEto.Account,
                ChainId = chain.Id,
                IsEnteredMarket = true,
                CTokenId = cToken.Id,
                TotalBorrowAmount = "0",
                AccumulativeBorrowComp = "0",
                AccumulativeSupplyComp = "0"
            });
        }
    }
}