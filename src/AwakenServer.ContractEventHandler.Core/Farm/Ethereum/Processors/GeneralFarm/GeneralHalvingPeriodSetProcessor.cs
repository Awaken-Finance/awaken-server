using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.BackgroundJob.Processors;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AwakenServer.ContractEventHandler.Farm.Ethereum.DTOs.GeneralFarm;
using AwakenServer.ContractEventHandler.Farm.Services;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Farm.Ethereum.Processors.GeneralFarm
{
    public class GeneralHalvingPeriodSetProcessor : EthereumEthereumEventProcessorBase<HalvingPeriodSet>
    {
        private readonly IRepository<Farms.Entities.Ef.Farm> _farmRepository;
        private readonly ICommonInfoCacheService _commonInfoCacheService;
        private readonly ILogger<GeneralHalvingPeriodSetProcessor> _logger;

        public GeneralHalvingPeriodSetProcessor(ILogger<GeneralHalvingPeriodSetProcessor> logger,
            IRepository<Farms.Entities.Ef.Farm> farmRepository,
            ICommonInfoCacheService commonInfoCacheService)
        {
            _logger = logger;
            _farmRepository = farmRepository;
            _commonInfoCacheService = commonInfoCacheService;
        }

        protected override async Task HandleEventAsync(
            HalvingPeriodSet eventDetailsEto,
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
            farm.MiningHalvingPeriod1 = eventDetailsEto.Period;
            await _farmRepository.UpdateAsync(farm);
        }
    }
}