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
    public class ClaimRevenueProcessor : EthereumEthereumEventProcessorBase<ClaimRevenue>
    {
        private readonly IRepository<FarmPool> _poolRepository;
        private readonly IRepository<FarmUserInfo> _farmUserInfosRepository;
        private readonly IRepository<FarmRecord> _recordRepository;
        private readonly ICommonInfoCacheService _commonInfoCacheService;
        private readonly ILogger<ClaimRevenueProcessor> _logger;

        public ClaimRevenueProcessor(ILogger<ClaimRevenueProcessor> logger,
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
            ClaimRevenue eventDetailsEto,
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
            var userInfo = await _farmUserInfosRepository.FirstOrDefaultAsync(x => x.User == eventDetailsEto.User);
            BehaviorType behaviorType;
            if (eventDetailsEto.DividendTokenType == DividendTokenType.ProjectToken)
            {
                behaviorType = BehaviorType.ClaimDistributedToken;
                userInfo.AccumulativeDividendProjectTokenAmount =
                    CalculationHelper.Add(userInfo.AccumulativeDividendProjectTokenAmount, eventDetailsEto.Amount);
            }
            else
            {
                behaviorType = BehaviorType.ClaimUsdt;
                userInfo.AccumulativeDividendUsdtAmount =
                    CalculationHelper.Add(userInfo.AccumulativeDividendUsdtAmount, eventDetailsEto.Amount);
            }

            await _farmUserInfosRepository.UpdateAsync(userInfo);
            await _recordRepository.InsertAsync(new FarmRecord
            {
                TransactionHash = contractEventDetailsDto.TransactionHash,
                User = eventDetailsEto.User,
                Amount = eventDetailsEto.Amount.ToString(),
                Date = DateTimeHelper.FromUnixTimeMilliseconds(contractEventDetailsDto.Timestamp * 1000),
                BehaviorType = behaviorType,
                PoolId = pool.Id,
                FarmId = farm.Id,
                TokenId = pool.SwapTokenId
            });
        }
    }
}