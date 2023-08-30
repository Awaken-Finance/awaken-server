using System;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using Awaken.Contracts.Farm;
using AwakenServer.ContractEventHandler.Farm.Services;
using AwakenServer.ContractEventHandler.Helpers;
using AwakenServer.Farms;
using AwakenServer.Farms.Entities.Ef;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Farm.AElf.Processors.MassiveFarm
{
    public class MassiveClaimRevenueProcessor : AElfEventProcessorBase<ClaimRevenue>
    {
        private readonly IRepository<FarmPool> _poolRepository;
        private readonly IRepository<FarmUserInfo> _farmUserInfosRepository;
        private readonly IRepository<FarmRecord> _recordRepository;
        private readonly ICommonInfoCacheService _commonInfoCacheService;
        public MassiveClaimRevenueProcessor(IRepository<FarmRecord> recordRepository,
            IRepository<FarmPool> poolRepository,
            IRepository<FarmUserInfo> farmUserInfosRepository, ICommonInfoCacheService commonInfoCacheService)
        {
            _recordRepository = recordRepository;
            _poolRepository = poolRepository;
            _farmUserInfosRepository = farmUserInfosRepository;
            _commonInfoCacheService = commonInfoCacheService;
        }

        protected override async Task HandleEventAsync(ClaimRevenue eventDetailsEto, EventContext txInfoDto)
        {
            var (_, farm) =
                await _commonInfoCacheService.GetCommonCacheInfoAsync(aelfChainId: txInfoDto.ChainId,
                    farmAddress: txInfoDto.EventAddress);
            var pool = await _poolRepository.FirstAsync(x => x.Pid == eventDetailsEto.Pid && x.FarmId == farm.Id);
            var userInfo =
                await _farmUserInfosRepository.SingleOrDefaultAsync(x =>
                    x.User == eventDetailsEto.User.ToBase58() && x.PoolId == pool.Id);
            if (userInfo == null)
            {
                throw new Exception(
                    $"Lack User Information in AppFarmUserInfo, User: {eventDetailsEto.User.ToBase58()}  PoolId: {pool.Id}");
            }
            BehaviorType behaviorType;
            if ((DividendTokenType) eventDetailsEto.TokenType == DividendTokenType.ProjectToken)
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
                TransactionHash = txInfoDto.TransactionId,
                User = eventDetailsEto.User.ToBase58(),
                Amount = eventDetailsEto.Amount.Value,
                Date = txInfoDto.BlockTime,
                BehaviorType = behaviorType,
                PoolId = pool.Id,
                FarmId = farm.Id,
                TokenId = pool.SwapTokenId
            });
        }
    }
}