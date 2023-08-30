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
    public class MarketEnteredProcessor : AElfEventProcessorBase<MarketEntered>
    {
        private readonly IRepository<CTokenUserInfo> _userInfoRepository;
        private readonly IChainAppService _chainAppService;
        private readonly ICachedDataProvider<CToken> _cTokenProvider;
        private readonly ILogger<MarketEnteredProcessor> _logger;

        public MarketEnteredProcessor(IRepository<CTokenUserInfo> userInfoRepository,
            IChainAppService chainAppService, ICachedDataProvider<CToken> cTokenProvider,
            ILogger<MarketEnteredProcessor> logger)
        {
            _userInfoRepository = userInfoRepository;
            _chainAppService = chainAppService;
            _cTokenProvider = cTokenProvider;
            _logger = logger;
        }

        protected override async Task HandleEventAsync(MarketEntered eventDetailsEto, EventContext txInfoDto)
        {
            _logger.LogInformation($"MarketEntered Trigger: {eventDetailsEto}");
            var chainId = txInfoDto.ChainId;
            var chain = await _chainAppService.GetByChainIdCacheAsync(chainId.ToString());
            var cToken = await _cTokenProvider.GetOrSetCachedDataAsync(chain.Id + eventDetailsEto.AToken.ToBase58(),
                x => x.ChainId == chain.Id && x.Address == eventDetailsEto.AToken.ToBase58());
            var targetUser = await _userInfoRepository.FirstOrDefaultAsync(x =>
                x.ChainId == chain.Id && x.User == eventDetailsEto.Account.ToBase58() &&
                x.CTokenId == cToken.Id);
            if (targetUser != null)
            {
                targetUser.IsEnteredMarket = true;
                await _userInfoRepository.UpdateAsync(targetUser);
                return;
            }

            await _userInfoRepository.InsertAsync(new CTokenUserInfo
            {
                User = eventDetailsEto.Account.ToBase58(),
                ChainId = chain.Id,
                IsEnteredMarket = true,
                CTokenId = cToken.Id,
                TotalBorrowAmount = "0",
                AccumulativeBorrowComp = "0",
                AccumulativeSupplyComp = "0"
            }, true);
        }
    }
}