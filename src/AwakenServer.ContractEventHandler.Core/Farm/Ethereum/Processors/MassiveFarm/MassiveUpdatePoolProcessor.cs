using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.BackgroundJob.Processors;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AwakenServer.ContractEventHandler.Farm.Ethereum.DTOs.MassiveFarm;
using AwakenServer.ContractEventHandler.Farm.Services;
using AwakenServer.ContractEventHandler.Helpers;
using AwakenServer.Farms.Entities.Ef;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Farm.Ethereum.Processors.MassiveFarm
{
    public class MassiveUpdatePoolProcessor : EthereumEthereumEventProcessorBase<UpdatePool>
    {
        private readonly ICommonInfoCacheService _commonInfoCacheService;
        private readonly IRepository<FarmPool> _poolRepository;
        private readonly ILogger<MassiveUpdatePoolProcessor> _logger;

        public MassiveUpdatePoolProcessor(ILogger<MassiveUpdatePoolProcessor> logger,
            IRepository<FarmPool> poolRepository,
            ICommonInfoCacheService commonInfoCacheService)
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
            var pool = await _poolRepository.FirstAsync(x => x.Pid == eventDetailsEto.Pid && x.FarmId == farm.Id);
            pool.AccumulativeDividendProjectToken =
                CalculationHelper.Add(pool.AccumulativeDividendProjectToken, eventDetailsEto.ProjectTokenAmount);
            pool.AccumulativeDividendUsdt =
                CalculationHelper.Add(pool.AccumulativeDividendUsdt, eventDetailsEto.UsdtAmount);
            pool.LastUpdateBlockHeight = pool.LastUpdateBlockHeight < eventDetailsEto.UpdateBlockHeight
                ? eventDetailsEto.UpdateBlockHeight
                : pool.LastUpdateBlockHeight;
            await _poolRepository.UpdateAsync(pool);
        }
    }
}