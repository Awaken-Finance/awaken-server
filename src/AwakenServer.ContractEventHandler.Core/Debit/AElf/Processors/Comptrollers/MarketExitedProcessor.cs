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
    public class MarketExitedProcessor: AElfEventProcessorBase<MarketExited>
    {
        private readonly IRepository<CTokenUserInfo> _userInfoRepository;
        private readonly IChainAppService _chainAppService;
        private readonly ICachedDataProvider<CToken> _cTokenProvider;
        private readonly ILogger<MarketExitedProcessor> _logger;

        public MarketExitedProcessor(IRepository<CTokenUserInfo> userInfoRepository,
            IChainAppService chainAppService, ICachedDataProvider<CToken> cTokenProvider, ILogger<MarketExitedProcessor> logger)
        {
            _userInfoRepository = userInfoRepository;
            _chainAppService = chainAppService;
            _cTokenProvider = cTokenProvider;
            _logger = logger;
        }

        protected override async Task HandleEventAsync(MarketExited eventDetailsEto, EventContext txInfoDto)
        {
            _logger.LogInformation($"MarketExited Trigger: {eventDetailsEto}");
            var chainId = txInfoDto.ChainId;
            var chain = await _chainAppService.GetByChainIdCacheAsync(chainId.ToString());
            var cToken = await _cTokenProvider.GetOrSetCachedDataAsync(chain.Id + eventDetailsEto.AToken.ToBase58(),
                x => x.ChainId == chain.Id && x.Address == eventDetailsEto.AToken.ToBase58());
            var targetUser = await _userInfoRepository.GetAsync(x =>
                x.ChainId == chain.Id && x.User == eventDetailsEto.Account.ToBase58() &&
                x.CTokenId == cToken.Id);
            targetUser.IsEnteredMarket = false;
            await _userInfoRepository.UpdateAsync(targetUser);
        }
    }
}