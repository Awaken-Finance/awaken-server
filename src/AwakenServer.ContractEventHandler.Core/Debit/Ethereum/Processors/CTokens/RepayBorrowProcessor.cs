using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.BackgroundJob.Processors;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.CToken;
using AwakenServer.ContractEventHandler.Debit.Helpers;
using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.Debits;
using AwakenServer.Debits.Entities.Ef;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Debit.Ethereum.Processors.CTokens
{
    public class RepayBorrowProcessor : EthereumEthereumEventProcessorBase<RepayBorrow>
    {
        private readonly IChainAppService _chainAppService;
        private readonly IRepository<CToken> _cTokenRepository;
        private readonly IRepository<CTokenRecord> _cTokenRecordRepository;
        private readonly IRepository<CTokenUserInfo> _userRepository;

        public RepayBorrowProcessor(IChainAppService chainAppService, IRepository<CToken> cTokenRepository,
            IRepository<CTokenUserInfo> userRepository, IRepository<CTokenRecord> cTokenRecordRepository)
        {
            _chainAppService = chainAppService;
            _cTokenRepository = cTokenRepository;
            _userRepository = userRepository;
            _cTokenRecordRepository = cTokenRecordRepository;
        }

        protected override async Task HandleEventAsync(RepayBorrow eventDetailsEto,
            ContractEventDetailsDto contractEventDetailsDto)
        {
            if (contractEventDetailsDto.StatusEnum != ContractEventStatus.Confirmed)
            {
                return;
            }

            var nodeName = contractEventDetailsDto.NodeName;
            var chain = await _chainAppService.GetByNameCacheAsync(nodeName);
            var cTokenInfo = await _cTokenRepository.GetAsync(x =>
                x.ChainId == chain.Id && x.Address == contractEventDetailsDto.Address);
            cTokenInfo.TotalUnderlyingAssetBorrowAmount = eventDetailsEto.TotalBorrows.ToString();
            await _cTokenRepository.UpdateAsync(cTokenInfo);
            var user = await _userRepository.GetAsync(x =>
                x.User == eventDetailsEto.Borrower && x.CTokenId == cTokenInfo.Id && x.ChainId == chain.Id);
            user.TotalBorrowAmount = eventDetailsEto.AccountBorrows.ToString();
            await _userRepository.UpdateAsync(user);

            var record1 = RecordGeneratorHelper.GenerateCTokenRecord(contractEventDetailsDto,
                cTokenInfo,
                eventDetailsEto.Payer, BehaviorType.Repay, eventDetailsEto.RepayAmount.ToString());
            var records = new List<CTokenRecord>
            {
                record1
            };
            if (eventDetailsEto.Payer != eventDetailsEto.Borrower)
            {
                var record2 = RecordGeneratorHelper.GenerateCTokenRecord(contractEventDetailsDto, cTokenInfo,
                    eventDetailsEto.Borrower, BehaviorType.Repaid, eventDetailsEto.RepayAmount.ToString());
                records.Add(record2);
            }

            await _cTokenRecordRepository.InsertManyAsync(records);
        }
    }
}