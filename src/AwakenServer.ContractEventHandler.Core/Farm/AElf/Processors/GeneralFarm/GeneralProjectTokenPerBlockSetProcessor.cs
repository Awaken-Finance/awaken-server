using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using Awaken.Contracts.PoolTwoContract;
using AwakenServer.ContractEventHandler.Farm.Services;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Farm.AElf.Processors.GeneralFarm
{
    public class GeneralProjectTokenPerBlockSetProcessor : AElfEventProcessorBase<DistributeTokenPerBlockSet>
    {
        private readonly ICommonInfoCacheService _commonInfoCacheService;
        private readonly IRepository<Farms.Entities.Ef.Farm> _farmRepository;

        public GeneralProjectTokenPerBlockSetProcessor(
            IRepository<Farms.Entities.Ef.Farm> farmRepository,
            ICommonInfoCacheService commonInfoCacheService)
        {
            _farmRepository = farmRepository;
            _commonInfoCacheService = commonInfoCacheService;
        }

        protected override async Task HandleEventAsync(DistributeTokenPerBlockSet eventDetailsEto,
            EventContext txInfoDto)
        {
            var (chain, _) = await _commonInfoCacheService.GetCommonCacheInfoAsync(aelfChainId: txInfoDto.ChainId);
            var farm = await _farmRepository.GetAsync(x =>
                x.FarmAddress == txInfoDto.EventAddress && x.ChainId == chain.Id);
            farm.ProjectTokenMinePerBlock1 = eventDetailsEto.NewDistributeTokenPerBlock.Value;
            await _farmRepository.UpdateAsync(farm);
        }
    }
}