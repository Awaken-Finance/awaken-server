using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using Awaken.Contracts.Controller;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.Helpers;
using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.Debits.Entities.Ef;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Debit.AElf.Processors.Comptrollers
{
    public class DistributedBorrowerCompProcessor : AElfEventProcessorBase<DistributedBorrowerPlatformToken>
    {
        //private readonly IChainAppService _chainAppService;
        private readonly  IChainAppService _chainAppService;
        private readonly IRepository<CToken> _cTokenRepository;
        private readonly IRepository<CTokenUserInfo> _userInfoRepository;
        private readonly ILogger<DistributedBorrowerCompProcessor> _logger;

        public DistributedBorrowerCompProcessor(
            IChainAppService chainAppService, IRepository<CToken> cTokenRepository,
            IRepository<CTokenUserInfo> userInfoRepository, ILogger<DistributedBorrowerCompProcessor> logger)
        {
            _chainAppService = chainAppService;
            _cTokenRepository = cTokenRepository;
            _userInfoRepository = userInfoRepository;
            _logger = logger;
        }

        protected override async Task HandleEventAsync(DistributedBorrowerPlatformToken eventDetailsEto,
            EventContext txInfoDto)
        {
            _logger.LogInformation($"DistributedBorrowerPlatformToken Trigger: {eventDetailsEto}");
            var chainId = txInfoDto.ChainId;
            var chain = await _chainAppService.GetByChainIdCacheAsync(chainId.ToString());
            var cToken =
                await _cTokenRepository.GetAsync(x =>
                    x.ChainId == chain.Id && x.Address == eventDetailsEto.AToken.ToBase58());
            cToken.AccumulativeBorrowComp =
                CalculationHelper.Add(cToken.AccumulativeBorrowComp, eventDetailsEto.PlatformTokenDelta);
            await _cTokenRepository.UpdateAsync(cToken);
            var user = await _userInfoRepository.GetAsync(x =>
                x.CTokenId == cToken.Id && x.User == eventDetailsEto.Borrower.ToBase58());
            user.AccumulativeBorrowComp =
                CalculationHelper.Add(user.AccumulativeBorrowComp, eventDetailsEto.PlatformTokenDelta);
            await _userInfoRepository.UpdateAsync(user);
        }
    }
}