using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using Awaken.Contracts.Farm;
using AwakenServer.ContractEventHandler.Farm.Services;
using AwakenServer.Farms.Entities.Ef;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Farm.AElf.Processors
{
    public class WeightSetProcessor : AElfEventProcessorBase<WeightSet>
    {
        private readonly ICommonInfoCacheService _commonInfoCacheService;
        private readonly IRepository<FarmPool> _poolRepository;
        private readonly IRepository<Farms.Entities.Ef.Farm> _farmRepository;

        public WeightSetProcessor(IRepository<FarmPool> poolRepository,
            IRepository<Farms.Entities.Ef.Farm> farmRepository,
            ICommonInfoCacheService commonInfoCacheService)
        {
            _poolRepository = poolRepository;
            _farmRepository = farmRepository;
            _commonInfoCacheService = commonInfoCacheService;
        }

        protected override async Task HandleEventAsync(WeightSet eventDetailsEto, EventContext txInfoDto)
        {
            var (_, farm) =
                await _commonInfoCacheService.GetCommonCacheInfoAsync(aelfChainId: txInfoDto.ChainId,
                    farmAddress: txInfoDto.EventAddress);
            var pool = await _poolRepository.FirstAsync(x => x.Pid == eventDetailsEto.Pid && x.FarmId == farm.Id);
            var preWeight = pool.Weight;
            pool.Weight = (int)eventDetailsEto.NewAllocationPoint;
            await _poolRepository.UpdateAsync(pool);
            var farmEntity = await _farmRepository.GetAsync(x => x.Id == farm.Id);
            farmEntity.TotalWeight = farmEntity.TotalWeight - preWeight + (int)eventDetailsEto.NewAllocationPoint;
            await _farmRepository.UpdateAsync(farmEntity);
        }
    }
}