using System.Numerics;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using Awaken.Contracts.Farm;
using AwakenServer.ContractEventHandler.Farm.Services;
using AwakenServer.ContractEventHandler.Helpers;
using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.Farms;
using AwakenServer.Farms.Entities.Ef;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Farm.AElf.Processors
{
    public class DepositProcessor : AElfEventProcessorBase<Deposit>
    {
        private readonly IRepository<FarmPool> _poolRepository;
        private readonly IRepository<FarmUserInfo> _farmUserInfosRepository;
        private readonly IRepository<FarmRecord> _recordRepository;
        private readonly ICommonInfoCacheService _commonInfoCacheService;
        private readonly ICachedDataProvider<FarmPool> _farmPoolProvider;

        public DepositProcessor(
            IRepository<FarmPool> poolRepository,
            IRepository<FarmUserInfo> farmUserInfosRepository,
            ICachedDataProvider<FarmPool> farmPoolProvider, IRepository<FarmRecord> recordRepository,
            ICommonInfoCacheService commonInfoCacheService)
        {
            _poolRepository = poolRepository;
            _farmUserInfosRepository = farmUserInfosRepository;
            _farmPoolProvider = farmPoolProvider;
            _recordRepository = recordRepository;
            _commonInfoCacheService = commonInfoCacheService;
        }

        protected override async Task HandleEventAsync(Deposit eventDetailsEto, EventContext txInfoDto)
        {
            var addDepositAmount = BigInteger.Parse(eventDetailsEto.Amount.ToString());
            if (addDepositAmount == BigInteger.Zero)
            {
                return;
            }

            var (chain, farm) =
                await _commonInfoCacheService.GetCommonCacheInfoAsync(aelfChainId: txInfoDto.ChainId,
                    farmAddress: txInfoDto.EventAddress);
            var pool = await _poolRepository.FirstAsync(x => x.Pid == eventDetailsEto.Pid && x.FarmId == farm.Id);
            await _farmPoolProvider.GetOrSetCachedDataByIdAsync(pool.Id);
            pool.TotalDepositAmount = CalculationHelper.Add(pool.TotalDepositAmount, addDepositAmount);
            await _poolRepository.UpdateAsync(pool);
            var user = eventDetailsEto.User.ToBase58();
            await _recordRepository.InsertAsync(new FarmRecord
            {
                TransactionHash = txInfoDto.TransactionId,
                User = user,
                Amount = eventDetailsEto.Amount.ToString(),
                Date = txInfoDto.BlockTime,
                BehaviorType = BehaviorType.Deposit,
                PoolId = pool.Id,
                FarmId = farm.Id,
                TokenId = pool.SwapTokenId
            });
            var userInfo =
                await _farmUserInfosRepository.FirstOrDefaultAsync(x =>
                    x.User == user && x.PoolId == pool.Id);
            if (userInfo != null)
            {
                userInfo.CurrentDepositAmount =
                    CalculationHelper.Add(userInfo.CurrentDepositAmount, addDepositAmount);
                await _farmUserInfosRepository.UpdateAsync(userInfo);
                return;
            }

            await _farmUserInfosRepository.InsertAsync(new FarmUserInfo
            {
                User = user,
                PoolId = pool.Id,
                ChainId = chain.Id,
                CurrentDepositAmount = addDepositAmount.ToString(),
                AccumulativeDividendProjectTokenAmount = "0",
                AccumulativeDividendUsdtAmount = "0"
            });
        }
    }
}