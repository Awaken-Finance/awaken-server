using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.BackgroundJob.Processors;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AwakenServer.ContractEventHandler.Farm.Ethereum.DTOs.GeneralFarm;
using AwakenServer.ContractEventHandler.Farm.Services;
using AwakenServer.ContractEventHandler.Helpers;
using AwakenServer.Farms.Entities.Ef;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Farm.Ethereum.Processors.GeneralFarm
{
    public class GeneralUpdatePoolProcessor: EthereumEthereumEventProcessorBase<UpdatePool>
    {
        private readonly IRepository<FarmPool> _poolRepository;
        private readonly ICommonInfoCacheService _commonInfoCacheService;
        private readonly ILogger<GeneralUpdatePoolProcessor> _logger;

        public GeneralUpdatePoolProcessor(ILogger<GeneralUpdatePoolProcessor> logger,
            IRepository<FarmPool> poolRepository, ICommonInfoCacheService commonInfoCacheService)
        {
            _logger = logger;
            _poolRepository = poolRepository;
            _commonInfoCacheService = commonInfoCacheService;
        }

        protected override async Task HandleEventAsync(
            UpdatePool eventDetailsEto,
            ContractEventDetailsDto contractEventDetailsDto)
        {
            if (contractEventDetailsDto.StatusEnum != ContractEventStatus.Confirmed)
            {
                return;
            }

            var nodeName = contractEventDetailsDto.NodeName;
            var (_, farm) =
                await _commonInfoCacheService.GetCommonCacheInfoAsync(nodeName, contractEventDetailsDto.Address);
            var pool = await _poolRepository.GetAsync(x => x.Pid == eventDetailsEto.Pid && x.FarmId == farm.Id);
            pool.AccumulativeDividendProjectToken =
                CalculationHelper.Add(pool.AccumulativeDividendProjectToken, eventDetailsEto.ProjectTokenAmount);
            pool.LastUpdateBlockHeight = pool.LastUpdateBlockHeight < eventDetailsEto.UpdateBlockHeight
                ? eventDetailsEto.UpdateBlockHeight
                : pool.LastUpdateBlockHeight;
            await _poolRepository.UpdateAsync(pool);
        }
    }
}