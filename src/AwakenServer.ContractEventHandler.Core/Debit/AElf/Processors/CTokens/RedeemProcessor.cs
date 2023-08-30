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
    public class RedeemProcessor : AElfEventProcessorBase<Redeem>
    {
        private readonly IChainAppService _chainAppService;
        private readonly IRepository<CToken> _cTokenRepository;
        private readonly IRepository<CTokenRecord> _cTokenRecordRepository;
        private readonly ILogger<RedeemProcessor> _logger;

        public RedeemProcessor(IRepository<CToken> cTokenRepository, IChainAppService chainAppService,
            IRepository<CTokenRecord> cTokenRecordRepository, ILogger<RedeemProcessor> logger)
        {
            _cTokenRepository = cTokenRepository;
            _chainAppService = chainAppService;
            _cTokenRecordRepository = cTokenRecordRepository;
            _logger = logger;
        }

        protected override async Task HandleEventAsync(Redeem eventDetailsEto, EventContext txInfoDto)
        {
            _logger.LogInformation($"Redeem Trigger: {eventDetailsEto}");
            var chainId = txInfoDto.ChainId;
            var chain = await _chainAppService.GetByChainIdCacheAsync(chainId.ToString());
            var cTokenInfo = await _cTokenRepository.GetAsync(x =>
                x.ChainId == chain.Id && x.Address == eventDetailsEto.AToken.ToBase58());
            cTokenInfo.TotalCTokenMintAmount =
                CalculationHelper.Minus(cTokenInfo.TotalCTokenMintAmount, eventDetailsEto.ATokenAmount);
            cTokenInfo.TotalUnderlyingAssetAmount =
                CalculationHelper.Minus(cTokenInfo.TotalUnderlyingAssetAmount, eventDetailsEto.UnderlyingAmount);
            await _cTokenRepository.UpdateAsync(cTokenInfo);
            var record = RecordGeneratorHelper.GenerateCTokenRecord(txInfoDto,
                cTokenInfo,
                eventDetailsEto.Sender.ToBase58(), BehaviorType.Redeem, eventDetailsEto.UnderlyingAmount.ToString(),
                eventDetailsEto.ATokenAmount.ToString());
            var records = new List<CTokenRecord>
            {
                record
            };

            await _cTokenRecordRepository.InsertManyAsync(records);
        }
    }
}