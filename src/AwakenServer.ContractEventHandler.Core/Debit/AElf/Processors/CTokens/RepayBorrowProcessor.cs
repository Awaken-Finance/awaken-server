using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using Awaken.Contracts.AToken;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.Debit.Helpers;
using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.Debits;
using AwakenServer.Debits.Entities.Ef;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Debit.AElf.Processors.CTokens
{
    public class RepayBorrowProcessor : AElfEventProcessorBase<RepayBorrow>
    {
        private readonly IChainAppService _chainAppService;
        private readonly IRepository<CToken> _cTokenRepository;
        private readonly IRepository<CTokenRecord> _cTokenRecordRepository;
        private readonly IRepository<CTokenUserInfo> _userRepository;
        private readonly ILogger<RepayBorrowProcessor> _logger;

        public RepayBorrowProcessor(IChainAppService chainAppService, IRepository<CToken> cTokenRepository,
            IRepository<CTokenUserInfo> userRepository, IRepository<CTokenRecord> cTokenRecordRepository, ILogger<RepayBorrowProcessor> logger)
        {
            _chainAppService = chainAppService;
            _cTokenRepository = cTokenRepository;
            _userRepository = userRepository;
            _cTokenRecordRepository = cTokenRecordRepository;
            _logger = logger;
        }

        protected override async Task HandleEventAsync(RepayBorrow eventDetailsEto, EventContext txInfoDto)
        {
            _logger.LogInformation($"RepayBorrow Trigger: {eventDetailsEto}");
            var chainId = txInfoDto.ChainId;
            var chain = await _chainAppService.GetByChainIdCacheAsync(chainId.ToString());
            var cTokenInfo = await _cTokenRepository.GetAsync(x =>
                x.ChainId == chain.Id && x.Address == eventDetailsEto.AToken.ToBase58());
            cTokenInfo.TotalUnderlyingAssetBorrowAmount = eventDetailsEto.TotalBorrows.ToString();
            await _cTokenRepository.UpdateAsync(cTokenInfo);
            var user = await _userRepository.GetAsync(x =>
                x.User == eventDetailsEto.Borrower.ToBase58() && x.CTokenId == cTokenInfo.Id && x.ChainId == chain.Id);
            user.TotalBorrowAmount = eventDetailsEto.BorrowBalance.ToString();
            await _userRepository.UpdateAsync(user);

            var record1 = RecordGeneratorHelper.GenerateCTokenRecord(txInfoDto,
                cTokenInfo,
                eventDetailsEto.Payer.ToBase58(), BehaviorType.Repay, eventDetailsEto.Amount.ToString());
            var records = new List<CTokenRecord>
            {
                record1
            };
            if (eventDetailsEto.Payer != eventDetailsEto.Borrower)
            {
                var record2 = RecordGeneratorHelper.GenerateCTokenRecord(txInfoDto,
                    cTokenInfo,
                    eventDetailsEto.Borrower.ToBase58(), BehaviorType.Repaid, eventDetailsEto.Amount.ToString());
                records.Add(record2);
            }

            await _cTokenRecordRepository.InsertManyAsync(records);
        }
    }
}