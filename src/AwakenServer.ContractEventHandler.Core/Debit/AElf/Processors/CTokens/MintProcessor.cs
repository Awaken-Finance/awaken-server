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
    public class MintProcessor : AElfEventProcessorBase<Mint>
    {
        private readonly IChainAppService _chainAppService;
        private readonly IRepository<CToken> _cTokenRepository;
        private readonly IRepository<CTokenRecord> _cTokenRecordRepository;
        private readonly ILogger<MintProcessor> _logger;
        private readonly IRepository<CTokenUserInfo> _userRepository;

        public MintProcessor(IChainAppService chainAppService, IRepository<CToken> cTokenRepository,
            IRepository<CTokenRecord> cTokenRecordRepository, ILogger<MintProcessor> logger,
            IRepository<CTokenUserInfo> userRepository)
        {
            _chainAppService = chainAppService;
            _cTokenRepository = cTokenRepository;
            _cTokenRecordRepository = cTokenRecordRepository;
            _logger = logger;
            _userRepository = userRepository;
        }

        protected override async Task HandleEventAsync(Mint eventDetailsEto, EventContext txInfoDto)
        {
            _logger.LogInformation($"Mint Trigger: {eventDetailsEto}");
            var chainId = txInfoDto.ChainId;
            var chain = await _chainAppService.GetByChainIdCacheAsync(chainId.ToString());
            _logger.LogInformation($"Chain Id: {chain.Id}");
            var cTokenInfo = await _cTokenRepository.GetAsync(x =>
                x.ChainId == chain.Id && x.Address == eventDetailsEto.AToken.ToBase58());
            cTokenInfo.TotalCTokenMintAmount =
                CalculationHelper.Add(cTokenInfo.TotalCTokenMintAmount, eventDetailsEto.ATokenAmount);
            cTokenInfo.TotalUnderlyingAssetAmount =
                CalculationHelper.Add(cTokenInfo.TotalUnderlyingAssetAmount, eventDetailsEto.UnderlyingAmount);
            await _cTokenRepository.UpdateAsync(cTokenInfo);
            
            var user = await _userRepository.FindAsync(x =>
                x.User == eventDetailsEto.Sender.ToBase58() && x.CTokenId == cTokenInfo.Id && x.ChainId == chain.Id);
            if (user == null)
            {
                await _userRepository.InsertAsync(new CTokenUserInfo
                {
                    User = eventDetailsEto.Sender.ToBase58(),
                    ChainId = chain.Id,
                    IsEnteredMarket = false,
                    CTokenId = cTokenInfo.Id,
                    TotalBorrowAmount = "0",
                    AccumulativeBorrowComp = "0",
                    AccumulativeSupplyComp = "0"
                }, true);
            }

            var record = RecordGeneratorHelper.GenerateCTokenRecord(txInfoDto,
                cTokenInfo,
                eventDetailsEto.Sender.ToBase58(), BehaviorType.Mint, eventDetailsEto.UnderlyingAmount.ToString(),
                eventDetailsEto.ATokenAmount.ToString(), eventDetailsEto.Channel);
            var records = new List<CTokenRecord>
            {
                record
            };
            await _cTokenRecordRepository.InsertManyAsync(records);
        }
    }
}