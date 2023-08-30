using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using Awaken.Contracts.Farm;
using AwakenServer.ContractEventHandler.Farm.Services;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Farm.AElf.Processors.MassiveFarm
{
    public class MassiveNewRewardSetProcessor : AElfEventProcessorBase<NewRewardSet>
    {
        private readonly ICommonInfoCacheService _commonInfoCacheService;
        private readonly IRepository<Farms.Entities.Ef.Farm> _farmRepository;

        public MassiveNewRewardSetProcessor(
            IRepository<Farms.Entities.Ef.Farm> farmRepository,
            ICommonInfoCacheService commonInfoCacheService)
        {
            _farmRepository = farmRepository;
            _commonInfoCacheService = commonInfoCacheService;
        }

        protected override async Task HandleEventAsync(NewRewardSet eventDetailsEto, EventContext txInfoDto)
        {
            var (chain, _) = await _commonInfoCacheService.GetCommonCacheInfoAsync(aelfChainId: txInfoDto.ChainId);
            var farm = await _farmRepository.GetAsync(x =>
                x.FarmAddress == txInfoDto.EventAddress && x.ChainId == chain.Id);
            farm.UsdtDividendPerBlock = eventDetailsEto.UsdtPerBlock.ToString();
            farm.UsdtDividendStartBlockHeight = eventDetailsEto.StartBlock;
            farm.UsdtDividendEndBlockHeight = eventDetailsEto.EndBlock;
            await _farmRepository.UpdateAsync(farm);
        }
    }
}