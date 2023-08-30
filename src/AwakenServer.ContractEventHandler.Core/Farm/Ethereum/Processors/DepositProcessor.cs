using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.BackgroundJob.Processors;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AwakenServer.ContractEventHandler.Farm.Ethereum.DTOs;
using AwakenServer.ContractEventHandler.Farm.Services;
using AwakenServer.ContractEventHandler.Helpers;
using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.Farms;
using AwakenServer.Farms.Entities.Ef;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Farm.Ethereum.Processors
{
    public class DepositProcessor : EthereumEthereumEventProcessorBase<Deposit>
    {
        private readonly IRepository<FarmPool> _poolRepository;
        private readonly IRepository<FarmUserInfo> _farmUserInfosRepository;
        private readonly IRepository<FarmRecord> _recordRepository;
        private readonly ICommonInfoCacheService _commonInfoCacheService;
        private readonly ICachedDataProvider<FarmPool> _farmPoolProvider;
        private readonly ILogger<DepositProcessor> _logger;

        public DepositProcessor(ILogger<DepositProcessor> logger,
            IRepository<FarmPool> poolRepository,
            IRepository<FarmUserInfo> farmUserInfosRepository,
            ICachedDataProvider<FarmPool> farmPoolProvider, IRepository<FarmRecord> recordRepository,
            ICommonInfoCacheService commonInfoCacheService)
        {
            _logger = logger;
            _poolRepository = poolRepository;
            _farmUserInfosRepository = farmUserInfosRepository;
            _farmPoolProvider = farmPoolProvider;
            _recordRepository = recordRepository;
            _commonInfoCacheService = commonInfoCacheService;
        }

        protected override async Task HandleEventAsync(
            Deposit eventDetailsEto,
            ContractEventDetailsDto contractEventDetailsDto)
        {
            if (contractEventDetailsDto.StatusEnum != ContractEventStatus.Confirmed)
            {
                return;
            }

            var addDepositAmount = eventDetailsEto.Amount;
            if (addDepositAmount == 0)
            {
                return;
            }

            var nodeName = contractEventDetailsDto.NodeName;
            var (chain, farm) =
                await _commonInfoCacheService.GetCommonCacheInfoAsync(nodeName, contractEventDetailsDto.Address);
            var pool = await _poolRepository.FirstAsync(x => x.Pid == eventDetailsEto.Pid && x.FarmId == farm.Id);
            await _farmPoolProvider.GetOrSetCachedDataByIdAsync(pool.Id);
            pool.TotalDepositAmount = CalculationHelper.Add(pool.TotalDepositAmount, addDepositAmount);
            await _poolRepository.UpdateAsync(pool);
            await _recordRepository.InsertAsync(new FarmRecord
            {
                TransactionHash = contractEventDetailsDto.TransactionHash,
                User = eventDetailsEto.User,
                Amount = eventDetailsEto.Amount.ToString(),
                Date = DateTimeHelper.FromUnixTimeMilliseconds(contractEventDetailsDto.Timestamp * 1000),
                BehaviorType = BehaviorType.Deposit,
                PoolId = pool.Id,
                FarmId = farm.Id,
                TokenId = pool.SwapTokenId
            });
            var userInfo =
                await _farmUserInfosRepository.FirstOrDefaultAsync(x =>
                    x.User == eventDetailsEto.User && x.PoolId == pool.Id);
            if (userInfo != null)
            {
                userInfo.CurrentDepositAmount =
                    CalculationHelper.Add(userInfo.CurrentDepositAmount, addDepositAmount);
                await _farmUserInfosRepository.UpdateAsync(userInfo);
                return;
            }

            await _farmUserInfosRepository.InsertAsync(new FarmUserInfo
            {
                User = eventDetailsEto.User,
                PoolId = pool.Id,
                ChainId = chain.Id,
                CurrentDepositAmount = addDepositAmount.ToString(),
                AccumulativeDividendProjectTokenAmount = "0",
                AccumulativeDividendUsdtAmount = "0"
            });
        }
    }
}