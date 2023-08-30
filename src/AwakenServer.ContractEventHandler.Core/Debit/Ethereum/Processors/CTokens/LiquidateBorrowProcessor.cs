using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.BackgroundJob.Processors;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.CToken;
using AwakenServer.ContractEventHandler.Debit.Helpers;
using AwakenServer.ContractEventHandler.Helpers;
using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.Debits;
using AwakenServer.Debits.Entities.Ef;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Debit.Ethereum.Processors.CTokens
{
    public class LiquidateBorrowProcessor : EthereumEthereumEventProcessorBase<LiquidateBorrow>
    {
        private readonly IRepository<CToken> _cTokenRepository;
        private readonly IChainAppService _chainAppService;
        private readonly IRepository<CTokenRecord> _cTokenRecordRepository;
        private readonly IRepository<CTokenUserInfo> _userRepository;

        public LiquidateBorrowProcessor(IRepository<CToken> cTokenRepository,
            IChainAppService chainAppService,
            IRepository<CTokenUserInfo> userRepository, IRepository<CTokenRecord> cTokenRecordRepository)
        {
            _cTokenRepository = cTokenRepository;
            _chainAppService = chainAppService;
            _userRepository = userRepository;
            _cTokenRecordRepository = cTokenRecordRepository;
        }

        protected override async Task HandleEventAsync(LiquidateBorrow eventDetailsEto,
            ContractEventDetailsDto contractEventDetailsDto)
        {
            if (contractEventDetailsDto.StatusEnum != ContractEventStatus.Confirmed)
            {
                return;
            }

            var nodeName = contractEventDetailsDto.NodeName;
            var chain = await _chainAppService.GetByNameCacheAsync(nodeName);
            var cTokenInfo = await _cTokenRepository.GetAsync(x =>
                x.ChainId == chain.Id && x.Address == eventDetailsEto.CTokenCollateral);
            cTokenInfo.TotalUnderlyingAssetBorrowAmount =
                CalculationHelper.Minus(cTokenInfo.TotalUnderlyingAssetBorrowAmount,
                    eventDetailsEto.RepayAmount);
            await _cTokenRepository.UpdateAsync(cTokenInfo);
            var user = await _userRepository.GetAsync(x =>
                x.User == eventDetailsEto.Borrower && x.CTokenId == cTokenInfo.Id && x.ChainId == chain.Id);
            user.TotalBorrowAmount =
                CalculationHelper.Minus(user.TotalBorrowAmount, eventDetailsEto.RepayAmount);
            await _userRepository.UpdateAsync(user);
            var record1 = RecordGeneratorHelper.GenerateCTokenRecord(contractEventDetailsDto,
                cTokenInfo,
                eventDetailsEto.Liquidator, BehaviorType.Liquidate, eventDetailsEto.RepayAmount.ToString(),
                eventDetailsEto.SeizeTokens.ToString());
            var record2 = RecordGeneratorHelper.GenerateCTokenRecord(contractEventDetailsDto,
                cTokenInfo,
                eventDetailsEto.Borrower, BehaviorType.Liquidated, eventDetailsEto.RepayAmount.ToString(),
                eventDetailsEto.SeizeTokens.ToString());
            var records = new List<CTokenRecord>
            {
                record1,
                record2
            };
            await _cTokenRecordRepository.InsertManyAsync(records);
        }
    }
}