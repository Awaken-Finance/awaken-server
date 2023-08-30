using System;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using AwakenServer.ContractEventHandler.Dividend.AElf.Services;
using AwakenServer.ContractEventHandler.Helpers;
using AwakenServer.Dividend;
using AwakenServer.Dividend.Entities.Ef;
using Awaken.Contracts.DividendPoolContract;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Dividend.AElf.Processors
{
    public class DepositProcessor : AElfEventProcessorBase<Deposit>
    {
        private readonly IDividendCacheService _dividendCacheService;
        private readonly IRepository<DividendPool> _dividendPoolRepository;
        private readonly IRepository<DividendUserPool> _userDividendInfoRepository;
        private readonly IRepository<DividendUserRecord> _recordRepository;
        private readonly ILogger<DepositProcessor> _logger;
        private readonly string _processorName = "DividendDepositProcessor"; // or conflict with farm's

        public DepositProcessor(
            IRepository<DividendPool> dividendPoolRepository,
            ILogger<DepositProcessor> logger,
            IRepository<DividendUserPool> userDividendInfoRepository,
            IRepository<DividendUserRecord> recordRepository, IDividendCacheService dividendCacheService)
        {
            _dividendPoolRepository = dividendPoolRepository;
            _logger = logger;
            _userDividendInfoRepository = userDividendInfoRepository;
            _recordRepository = recordRepository;
            _dividendCacheService = dividendCacheService;
        }

        public override string GetProcessorName()
        {
            return _processorName;
        }

        protected override async Task HandleEventAsync(Deposit eventDetailsEto, EventContext txInfoDto)
        {
            var chain = await _dividendCacheService.GetCachedChainAsync(txInfoDto.ChainId);
            var dividendBaseInfo =
                await _dividendCacheService.GetDividendBaseInfoAsync(chain.Id, txInfoDto.EventAddress);
            var dividendPool =
                await _dividendPoolRepository.GetAsync(x =>
                    x.DividendId == dividendBaseInfo.Id && x.Pid == eventDetailsEto.Pid);
            var user = eventDetailsEto.User.ToBase58();

            // update pool
            dividendPool.DepositAmount =
                CalculationHelper.Add(dividendPool.DepositAmount, eventDetailsEto.Amount.Value);
            await _dividendPoolRepository.UpdateAsync(dividendPool);

            // add record
            await AddDepositRecordAsync(user, eventDetailsEto.Amount.Value, chain.Id, dividendPool.Id, txInfoDto);

            // modify user info
            var userInfo = await _userDividendInfoRepository.FindAsync(x =>
                x.PoolId == dividendPool.Id && x.User == user);
            if (userInfo == null)
            {
                await _userDividendInfoRepository.InsertAsync(new DividendUserPool
                {
                    ChainId = chain.Id,
                    PoolId = dividendPool.Id,
                    User = eventDetailsEto.User.ToBase58(),
                    DepositAmount = eventDetailsEto.Amount.Value
                });
                return;
            }

            userInfo.DepositAmount = CalculationHelper.Add(userInfo.DepositAmount, eventDetailsEto.Amount.Value);
            await _userDividendInfoRepository.UpdateAsync(userInfo);
        }

        private async Task AddDepositRecordAsync(string user, string amount, string chainId, Guid poolId,
            EventContext txInfoDto)
        {
            await _recordRepository.InsertAsync(new DividendUserRecord
            {
                ChainId = chainId,
                TransactionHash = txInfoDto.TransactionId,
                User = user,
                DateTime = txInfoDto.BlockTime,
                Amount = amount,
                BehaviorType = BehaviorType.Deposit,
                PoolId = poolId,
                DividendTokenId = Guid.Empty
            });
        }
    }
}