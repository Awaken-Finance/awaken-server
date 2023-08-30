using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using AwakenServer.ContractEventHandler.Farm.Services;
using Awaken.Contracts.PoolTwoContract;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Farm.AElf.Processors.GeneralFarm
{
    public class GeneralHalvingPeriodSetProcessor : AElfEventProcessorBase<HalvingPeriodSet>
    {
        private readonly IRepository<Farms.Entities.Ef.Farm> _farmRepository;
        private readonly ICommonInfoCacheService _commonInfoCacheService;

        public GeneralHalvingPeriodSetProcessor(
            IRepository<Farms.Entities.Ef.Farm> farmRepository,
            ICommonInfoCacheService commonInfoCacheService)
        {
            _farmRepository = farmRepository;
            _commonInfoCacheService = commonInfoCacheService;
        }

        protected override async Task HandleEventAsync(HalvingPeriodSet eventDetailsEto, EventContext txInfoDto)
        {
            var (chain, _) = await _commonInfoCacheService.GetCommonCacheInfoAsync(aelfChainId: txInfoDto.ChainId);
            var farm = await _farmRepository.GetAsync(x =>
                x.FarmAddress == txInfoDto.EventAddress && x.ChainId == chain.Id);
            farm.MiningHalvingPeriod1 = eventDetailsEto.Period;
            await _farmRepository.UpdateAsync(farm);
        }
    }
}