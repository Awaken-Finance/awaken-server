using System;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using AwakenServer.ContractEventHandler.Dividend.AElf.Services;
using AwakenServer.ContractEventHandler.Helpers;
using AwakenServer.Dividend;
using AwakenServer.Dividend.Entities.Ef;
using Awaken.Contracts.DividendPoolContract;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Dividend.AElf.Processors
{
    public class WithdrawProcessor : AElfEventProcessorBase<Withdraw>
    {
        private readonly IDividendCacheService _dividendCacheService;
        private readonly IRepository<DividendPool> _dividendPoolRepository;
        private readonly IRepository<DividendUserPool> _userDividendInfoRepository;
        private readonly IRepository<DividendUserRecord> _recordRepository;
        private readonly string _processorName = "DividendWithdrawProcessor";

        public WithdrawProcessor(IDividendCacheService dividendCacheService,
            IRepository<DividendPool> dividendPoolRepository, IRepository<DividendUserPool> userDividendInfoRepository,
            IRepository<DividendUserRecord> recordRepository)
        {
            _dividendCacheService = dividendCacheService;
            _dividendPoolRepository = dividendPoolRepository;
            _userDividendInfoRepository = userDividendInfoRepository;
            _recordRepository = recordRepository;
        }
        
        public override string GetProcessorName()
        {
            return _processorName;
        }

        protected override async Task HandleEventAsync(Withdraw eventDetailsEto, EventContext txInfoDto)
        {
            var chain = await _dividendCacheService.GetCachedChainAsync(txInfoDto.ChainId);
            var dividendBaseInfo =
                await _dividendCacheService.GetDividendBaseInfoAsync(chain.Id, txInfoDto.EventAddress);
            var dividendPool =
                await _dividendPoolRepository.GetAsync(x =>
                    x.DividendId == dividendBaseInfo.Id && x.Pid == eventDetailsEto.Pid);
            var user = eventDetailsEto.User.ToBase58();

            var withdrawAmount = eventDetailsEto.Amount.Value;
            // update pool
            dividendPool.DepositAmount = CalculationHelper.Minus(dividendPool.DepositAmount, withdrawAmount);
            await _dividendPoolRepository.UpdateAsync(dividendPool);

            // add record
            await AddWithdrawRecordAsync(user, withdrawAmount, chain.Id, dividendPool.Id, txInfoDto);

            // modify user info
            var userInfo = await _userDividendInfoRepository.FindAsync(x =>
                x.PoolId == dividendPool.Id && x.User == user);
            if (userInfo == null)
            {
                throw new Exception(
                    $"Lack user deposit information, Dividend: {txInfoDto.EventAddress}, User: {user} , Pid: {dividendPool.Pid}");
            }

            userInfo.DepositAmount = CalculationHelper.Minus(userInfo.DepositAmount, withdrawAmount);
            await _userDividendInfoRepository.UpdateAsync(userInfo);
        }

        private async Task AddWithdrawRecordAsync(string user, string amount, string chainId, Guid poolId,
            EventContext txInfoDto)
        {
            await _recordRepository.InsertAsync(new DividendUserRecord
            {
                ChainId = chainId,
                TransactionHash = txInfoDto.TransactionId,
                User = user,
                DateTime = txInfoDto.BlockTime,
                Amount = amount,
                BehaviorType = BehaviorType.Withdraw,
                PoolId = poolId,
                DividendTokenId = Guid.Empty
            });
        }
    }
}