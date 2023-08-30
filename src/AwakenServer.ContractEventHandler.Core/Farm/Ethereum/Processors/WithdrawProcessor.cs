using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.BackgroundJob.Processors;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AwakenServer.ContractEventHandler.Farm.Ethereum.DTOs;
using AwakenServer.ContractEventHandler.Farm.Services;
using AwakenServer.ContractEventHandler.Helpers;
using AwakenServer.Farms;
using AwakenServer.Farms.Entities.Ef;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Farm.Ethereum.Processors
{
    public class WithdrawProcessor : EthereumEthereumEventProcessorBase<Withdraw>
    {
        private readonly ICommonInfoCacheService _commonInfoCacheService;
        private readonly IRepository<FarmPool> _poolRepository;
        private readonly IRepository<FarmUserInfo> _farmUserInfosRepository;
        private readonly IRepository<FarmRecord> _recordRepository;
        private readonly ILogger<WithdrawProcessor> _logger;

        public WithdrawProcessor(ILogger<WithdrawProcessor> logger,
            IRepository<FarmPool> poolRepository,
            IRepository<FarmUserInfo> farmUserInfosRepository,
            IRepository<FarmRecord> recordRepository, ICommonInfoCacheService commonInfoCacheService)
        {
            _logger = logger;
            _poolRepository = poolRepository;
            _farmUserInfosRepository = farmUserInfosRepository;
            _recordRepository = recordRepository;
            _commonInfoCacheService = commonInfoCacheService;
        }

        protected override async Task HandleEventAsync(
            Withdraw eventDetailsEto,
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
            pool.TotalDepositAmount =
                CalculationHelper.Minus(pool.TotalDepositAmount, eventDetailsEto.Amount);
            await _poolRepository.UpdateAsync(pool);
            var userInfo =
                await _farmUserInfosRepository.FirstAsync(x =>
                    x.User == eventDetailsEto.User && x.PoolId == pool.Id);
            userInfo.CurrentDepositAmount =
                CalculationHelper.Minus(userInfo.CurrentDepositAmount, eventDetailsEto.Amount);
            await _recordRepository.InsertAsync(new FarmRecord
            {
                TransactionHash = contractEventDetailsDto.TransactionHash,
                User = eventDetailsEto.User,
                Amount = eventDetailsEto.Amount.ToString(),
                Date = DateTimeHelper.FromUnixTimeMilliseconds(contractEventDetailsDto.Timestamp * 1000),
                BehaviorType = BehaviorType.Withdraw,
                PoolId = pool.Id,
                FarmId = farm.Id,
                TokenId = pool.SwapTokenId
            });
            await _farmUserInfosRepository.UpdateAsync(userInfo);
        }
    }
}