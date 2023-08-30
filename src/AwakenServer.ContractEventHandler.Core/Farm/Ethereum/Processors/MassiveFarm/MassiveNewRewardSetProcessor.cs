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
    public class MassiveNewRewardSetProcessor: EthereumEthereumEventProcessorBase<NewRewardSet>
    {
        private readonly ICommonInfoCacheService _commonInfoCacheService;
        private readonly IRepository<Farms.Entities.Ef.Farm> _farmRepository;
        private readonly ILogger<MassiveNewRewardSetProcessor> _logger;

        public MassiveNewRewardSetProcessor(ILogger<MassiveNewRewardSetProcessor> logger,
            IRepository<Farms.Entities.Ef.Farm> farmRepository,
            ICommonInfoCacheService commonInfoCacheService)
        {
            _logger = logger;
            _farmRepository = farmRepository;
            _commonInfoCacheService = commonInfoCacheService;
        }

        protected override async Task HandleEventAsync(
            NewRewardSet eventDetailsEto,
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
            farm.UsdtDividendPerBlock = eventDetailsEto.UsdtPerBlock.ToString();
            farm.UsdtDividendStartBlockHeight = eventDetailsEto.StartBlock;
            farm.UsdtDividendEndBlockHeight = eventDetailsEto.EndBlock;
            await _farmRepository.UpdateAsync(farm);
        }
    }
}