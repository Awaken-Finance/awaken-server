using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using Awaken.Contracts.AToken;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.Debit.Helpers;
using AwakenServer.ContractEventHandler.Helpers;
using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.Debits;
using AwakenServer.Debits.Entities.Ef;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Debit.AElf.Processors.CTokens
{
    public class LiquidateBorrowProcessor : AElfEventProcessorBase<LiquidateBorrow>
    {
        private readonly IRepository<CToken> _cTokenRepository;
        private readonly IChainAppService _chainAppService;
        private readonly IRepository<CTokenRecord> _cTokenRecordRepository;
        private readonly IRepository<CTokenUserInfo> _userRepository;
        private readonly ILogger<LiquidateBorrowProcessor> _logger;

        public LiquidateBorrowProcessor(IRepository<CToken> cTokenRepository,
            IChainAppService chainAppService,
            IRepository<CTokenUserInfo> userRepository, IRepository<CTokenRecord> cTokenRecordRepository,
            ILogger<LiquidateBorrowProcessor> logger)
        {
            _cTokenRepository = cTokenRepository;
            _chainAppService = chainAppService;
            _userRepository = userRepository;
            _cTokenRecordRepository = cTokenRecordRepository;
            _logger = logger;
        }

        protected override async Task HandleEventAsync(LiquidateBorrow eventDetailsEto, EventContext txInfoDto)
        {
            _logger.LogInformation($"LiquidateBorrow Trigger: {eventDetailsEto}");
            var chainId = txInfoDto.ChainId;
            var chain = await _chainAppService.GetByChainIdCacheAsync(chainId.ToString());
            var cTokenInfo = await _cTokenRepository.GetAsync(x =>
                x.ChainId == chain.Id && x.Address == eventDetailsEto.RepayAToken.ToBase58());
            cTokenInfo.TotalUnderlyingAssetBorrowAmount =
                CalculationHelper.Minus(cTokenInfo.TotalUnderlyingAssetBorrowAmount,
                    eventDetailsEto.RepayAmount);
            await _cTokenRepository.UpdateAsync(cTokenInfo);
            var user = await _userRepository.GetAsync(x =>
                x.User == eventDetailsEto.Borrower.ToBase58() && x.CTokenId == cTokenInfo.Id && x.ChainId == chain.Id);
            user.TotalBorrowAmount =
                CalculationHelper.Minus(user.TotalBorrowAmount, eventDetailsEto.RepayAmount);
            await _userRepository.UpdateAsync(user);
            var record1 = RecordGeneratorHelper.GenerateCTokenRecord(txInfoDto,
                cTokenInfo,
                eventDetailsEto.Liquidator.ToBase58(), BehaviorType.Liquidate, eventDetailsEto.RepayAmount.ToString(),
                eventDetailsEto.SeizeTokenAmount.ToString());
            var record2 = RecordGeneratorHelper.GenerateCTokenRecord(txInfoDto,
                cTokenInfo,
                eventDetailsEto.Borrower.ToBase58(), BehaviorType.Liquidated, eventDetailsEto.RepayAmount.ToString(),
                eventDetailsEto.SeizeTokenAmount.ToString());
            var records = new List<CTokenRecord>
            {
                record1,
                record2
            };
            await _cTokenRecordRepository.InsertManyAsync(records);
        }
    }
}