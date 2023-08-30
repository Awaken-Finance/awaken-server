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
    public class BorrowProcessor : EthereumEthereumEventProcessorBase<Borrow>
    {
        private readonly IRepository<CToken> _cTokenRepository;
        private readonly IRepository<CTokenUserInfo> _userRepository;
        private readonly IChainAppService _chainAppService;
        private readonly IRepository<CTokenRecord> _cTokenRecordRepository;

        public BorrowProcessor(IRepository<CToken> cTokenRepository, IChainAppService chainAppService,
            IRepository<CTokenUserInfo> userRepository, IRepository<CTokenRecord> cTokenRecordRepository)
        {
            _cTokenRepository = cTokenRepository;
            _chainAppService = chainAppService;
            _userRepository = userRepository;
            _cTokenRecordRepository = cTokenRecordRepository;
        }

        protected override async Task HandleEventAsync(Borrow eventDetailsEto,
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
            var user = await _userRepository.FindAsync(x =>
                x.User == eventDetailsEto.Borrower && x.CTokenId == cTokenInfo.Id && x.ChainId == chain.Id);
            if (user != null)
            {
                user.TotalBorrowAmount = eventDetailsEto.AccountBorrows.ToString();
                await _userRepository.UpdateAsync(user);
            }
            else
            {
                await _userRepository.InsertAsync(new CTokenUserInfo
                {
                    User = eventDetailsEto.Borrower,
                    ChainId = chain.Id,
                    IsEnteredMarket = true,
                    CTokenId = cTokenInfo.Id,
                    TotalBorrowAmount = eventDetailsEto.AccountBorrows.ToString(),
                    AccumulativeBorrowComp = "0",
                    AccumulativeSupplyComp = "0"
                });
            }

            var record = RecordGeneratorHelper.GenerateCTokenRecord(contractEventDetailsDto,
                cTokenInfo,
                eventDetailsEto.Borrower, BehaviorType.Borrow, eventDetailsEto.BorrowAmount.ToString(),
                channel: eventDetailsEto.Channel);
            var records = new List<CTokenRecord>
            {
                record
            };
            await _cTokenRecordRepository.InsertManyAsync(records);
        }
    }
}