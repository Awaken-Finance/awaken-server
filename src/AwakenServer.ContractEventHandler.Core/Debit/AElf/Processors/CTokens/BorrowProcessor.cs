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
    public class BorrowProcessor : AElfEventProcessorBase<Borrow>
    {
        private readonly IRepository<CToken> _cTokenRepository;
        private readonly IRepository<CTokenUserInfo> _userRepository;
        //private readonly IChainAppService _chainAppService;
        private readonly  IChainAppService _chainAppService;
        private readonly IRepository<CTokenRecord> _cTokenRecordRepository;
        private readonly ILogger<BorrowProcessor> _logger;

        public BorrowProcessor(IRepository<CToken> cTokenRepository, IChainAppService chainAppService,
            IRepository<CTokenUserInfo> userRepository, IRepository<CTokenRecord> cTokenRecordRepository, ILogger<BorrowProcessor> logger)
        {
            _cTokenRepository = cTokenRepository;
            _chainAppService = chainAppService;
            _userRepository = userRepository;
            _cTokenRecordRepository = cTokenRecordRepository;
            _logger = logger;
        }

        protected override async Task HandleEventAsync(Borrow eventDetailsEto, EventContext txInfoDto)
        {
            _logger.LogInformation($"Borrow Trigger: {eventDetailsEto}");
            var chainId = txInfoDto.ChainId;
            var chain = await _chainAppService.GetByChainIdCacheAsync(chainId.ToString());
            var cTokenInfo = await _cTokenRepository.GetAsync(x =>
                x.ChainId == chain.Id && x.Address == eventDetailsEto.AToken.ToBase58());
            cTokenInfo.TotalUnderlyingAssetBorrowAmount = eventDetailsEto.TotalBorrows.ToString();
            await _cTokenRepository.UpdateAsync(cTokenInfo);
            var user = await _userRepository.FindAsync(x =>
                x.User == eventDetailsEto.Borrower.ToBase58() && x.CTokenId == cTokenInfo.Id && x.ChainId == chain.Id);
            if (user != null)
            {
                user.TotalBorrowAmount = eventDetailsEto.BorrowBalance.ToString();
                await _userRepository.UpdateAsync(user);
            }
            else
            {
                await _userRepository.InsertAsync(new CTokenUserInfo
                {
                    User = eventDetailsEto.Borrower.ToBase58(),
                    ChainId = chain.Id,
                    IsEnteredMarket = true,
                    CTokenId = cTokenInfo.Id,
                    TotalBorrowAmount = eventDetailsEto.BorrowBalance.ToString(),
                    AccumulativeBorrowComp = "0",
                    AccumulativeSupplyComp = "0"
                }, true);
            }
            
            var record = RecordGeneratorHelper.GenerateCTokenRecord(txInfoDto, cTokenInfo,
                eventDetailsEto.Borrower.ToBase58(), BehaviorType.Borrow, eventDetailsEto.Amount.ToString());
            var records = new List<CTokenRecord>
            {
                record
            };
            await _cTokenRecordRepository.InsertManyAsync(records);
        }
    }
}