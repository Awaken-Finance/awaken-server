using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using Awaken.Contracts.Farm;
using AwakenServer.ContractEventHandler.Farm.Services;
using AwakenServer.ContractEventHandler.Helpers;
using AwakenServer.Farms;
using AwakenServer.Farms.Entities.Ef;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Farm.AElf.Processors
{
    public class WithdrawProcessor : AElfEventProcessorBase<Withdraw>
    {
        private readonly ICommonInfoCacheService _commonInfoCacheService;
        private readonly IRepository<FarmPool> _poolRepository;
        private readonly IRepository<FarmUserInfo> _farmUserInfosRepository;
        private readonly IRepository<FarmRecord> _recordRepository;

        public WithdrawProcessor(
            IRepository<FarmPool> poolRepository,
            IRepository<FarmUserInfo> farmUserInfosRepository,
            IRepository<FarmRecord> recordRepository, ICommonInfoCacheService commonInfoCacheService)
        {
            _poolRepository = poolRepository;
            _farmUserInfosRepository = farmUserInfosRepository;
            _recordRepository = recordRepository;
            _commonInfoCacheService = commonInfoCacheService;
        }

        protected override async Task HandleEventAsync(Withdraw eventDetailsEto, EventContext txInfoDto)
        {
            var (_, farm) =
                await _commonInfoCacheService.GetCommonCacheInfoAsync(aelfChainId: txInfoDto.ChainId,
                    farmAddress: txInfoDto.EventAddress);
            var pool = await _poolRepository.FirstAsync(x => x.Pid == eventDetailsEto.Pid && x.FarmId == farm.Id);
            pool.TotalDepositAmount =
                CalculationHelper.Minus(pool.TotalDepositAmount, eventDetailsEto.Amount);
            await _poolRepository.UpdateAsync(pool);
            var user = eventDetailsEto.User.ToBase58();
            var userInfo =
                await _farmUserInfosRepository.FirstAsync(x =>
                    x.User == user && x.PoolId == pool.Id);
            userInfo.CurrentDepositAmount =
                CalculationHelper.Minus(userInfo.CurrentDepositAmount, eventDetailsEto.Amount);
            await _recordRepository.InsertAsync(new FarmRecord
            {
                TransactionHash = txInfoDto.TransactionId,
                User = user,
                Amount = eventDetailsEto.Amount.ToString(),
                Date = txInfoDto.BlockTime,
                BehaviorType = BehaviorType.Withdraw,
                PoolId = pool.Id,
                FarmId = farm.Id,
                TokenId = pool.SwapTokenId
            });
            await _farmUserInfosRepository.UpdateAsync(userInfo);
        }
    }
}