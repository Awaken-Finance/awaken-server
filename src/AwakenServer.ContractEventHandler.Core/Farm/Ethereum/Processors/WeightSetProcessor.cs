using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.BackgroundJob.Processors;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AwakenServer.ContractEventHandler.Farm.Ethereum.DTOs;
using AwakenServer.ContractEventHandler.Farm.Services;
using AwakenServer.Farms.Entities.Ef;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Farm.Ethereum.Processors
{
    public class WeightSetProcessor : EthereumEthereumEventProcessorBase<WeightSet>
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

        protected override async Task HandleEventAsync(
            WeightSet eventDetailsEto,
            ContractEventDetailsDto contractEventDetailsDto)
        {
            if (contractEventDetailsDto.StatusEnum != ContractEventStatus.Confirmed)
            {
                return;
            }

            var nodeName = contractEventDetailsDto.NodeName;
            var (_, farm) =
                await _commonInfoCacheService.GetCommonCacheInfoAsync(nodeName, contractEventDetailsDto.Address);
            var pool = await _poolRepository.FirstAsync(x => x.Pid == eventDetailsEto.Pid && x.FarmId == farm.Id);
            var preWeight = pool.Weight;
            pool.Weight = eventDetailsEto.NewAllocationPoint;
            await _poolRepository.UpdateAsync(pool);
            var farmEntity = await _farmRepository.GetAsync(x => x.Id == farm.Id);
            farmEntity.TotalWeight = farmEntity.TotalWeight - preWeight + eventDetailsEto.NewAllocationPoint;
            await _farmRepository.UpdateAsync(farmEntity);
        }
    }
}