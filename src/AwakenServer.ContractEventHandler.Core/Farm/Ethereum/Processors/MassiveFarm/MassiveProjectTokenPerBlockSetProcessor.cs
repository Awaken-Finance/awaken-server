using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.BackgroundJob.Processors;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AwakenServer.ContractEventHandler.Farm.Ethereum.DTOs.MassiveFarm;
using AwakenServer.ContractEventHandler.Farm.Services;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Farm.Ethereum.Processors.MassiveFarm
{
    public class MassiveProjectTokenPerBlockSetProcessor : EthereumEthereumEventProcessorBase<ProjectTokenPerBlockSet>
    {
        private readonly IRepository<Farms.Entities.Ef.Farm> _farmRepository;
        private readonly ICommonInfoCacheService _commonInfoCacheService;
        private readonly ILogger<MassiveProjectTokenPerBlockSetProcessor> _logger;
        

        public MassiveProjectTokenPerBlockSetProcessor(ILogger<MassiveProjectTokenPerBlockSetProcessor> logger,
            IRepository<Farms.Entities.Ef.Farm> farmRepository, ICommonInfoCacheService commonInfoCacheService)
        {
            _logger = logger;
            _farmRepository = farmRepository;
            _commonInfoCacheService = commonInfoCacheService;
        }

        protected override async Task HandleEventAsync(
            ProjectTokenPerBlockSet eventDetailsEto,
            ContractEventDetailsDto contractEventDetailsDto)
        {
            if (contractEventDetailsDto.StatusEnum != ContractEventStatus.Confirmed)
            {
                return;
            }

            var nodeName = contractEventDetailsDto.NodeName;
            var (chain, _) = await _commonInfoCacheService.GetCommonCacheInfoAsync(nodeName);
            var farm = await _farmRepository.GetAsync(x =>
                x.FarmAddress == contractEventDetailsDto.Address && x.ChainId == chain.Id);
            farm.ProjectTokenMinePerBlock1 = eventDetailsEto.NewProjectTokenPerBlock1.ToString();
            farm.ProjectTokenMinePerBlock2 = eventDetailsEto.NewProjectTokenPerBlock2.ToString();
            await _farmRepository.UpdateAsync(farm);
        }
    }
}