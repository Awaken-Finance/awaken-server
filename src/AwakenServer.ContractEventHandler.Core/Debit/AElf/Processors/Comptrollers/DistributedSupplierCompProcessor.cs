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
    public class DistributedSupplierCompProcessor : AElfEventProcessorBase<DistributedSupplierPlatformToken>
    {
        private readonly IChainAppService _chainAppService;
        private readonly IRepository<CToken> _cTokenRepository;
        private readonly IRepository<CTokenUserInfo> _userInfoRepository;
        private readonly ILogger<DistributedSupplierCompProcessor> _logger;

        public DistributedSupplierCompProcessor(IChainAppService chainAppService,
            IRepository<CToken> cTokenRepository, IRepository<CTokenUserInfo> userInfoRepository,
            ILogger<DistributedSupplierCompProcessor> logger)
        {
            _chainAppService = chainAppService;
            _cTokenRepository = cTokenRepository;
            _userInfoRepository = userInfoRepository;
            _logger = logger;
        }

        protected override async Task HandleEventAsync(DistributedSupplierPlatformToken eventDetailsEto,
            EventContext txInfoDto)
        {
            _logger.LogInformation($"DistributedSupplierPlatformToken Trigger: {eventDetailsEto}");
            var chainId = txInfoDto.ChainId;
            var chain = await _chainAppService.GetByChainIdCacheAsync(chainId.ToString());
            var cToken =
                await _cTokenRepository.GetAsync(x =>
                    x.ChainId == chain.Id && x.Address == eventDetailsEto.AToken.ToBase58());
            cToken.AccumulativeSupplyComp =
                CalculationHelper.Add(cToken.AccumulativeSupplyComp, eventDetailsEto.PlatformTokenDelta);
            await _cTokenRepository.UpdateAsync(cToken);
            var user = await _userInfoRepository.FindAsync(x =>
                x.CTokenId == cToken.Id && x.User == eventDetailsEto.Supplier.ToBase58());
            if (user != null)
            {
                user.AccumulativeSupplyComp =
                    CalculationHelper.Add(user.AccumulativeSupplyComp, eventDetailsEto.PlatformTokenDelta);
                await _userInfoRepository.UpdateAsync(user);
                return;
            }
            
            await _userInfoRepository.InsertAsync(new CTokenUserInfo
            {
                User = eventDetailsEto.Supplier.ToBase58(),
                ChainId = chain.Id,
                IsEnteredMarket = true,
                CTokenId = cToken.Id,
                TotalBorrowAmount = "0",
                AccumulativeBorrowComp = "0",
                AccumulativeSupplyComp = eventDetailsEto.PlatformTokenDelta.Value
            });
        }
    }
}