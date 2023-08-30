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
    public class RedeemProcessor : EthereumEthereumEventProcessorBase<Redeem>
    {
        private readonly IChainAppService _chainAppService;
        private readonly IRepository<CToken> _cTokenRepository;
        private readonly IRepository<CTokenRecord> _cTokenRecordRepository;

        public RedeemProcessor(IRepository<CToken> cTokenRepository, IChainAppService chainAppService,
            IRepository<CTokenRecord> cTokenRecordRepository)
        {
            _cTokenRepository = cTokenRepository;
            _chainAppService = chainAppService;
            _cTokenRecordRepository = cTokenRecordRepository;
        }

        protected override async Task HandleEventAsync(Redeem eventDetailsEto,
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
            cTokenInfo.TotalCTokenMintAmount =
                CalculationHelper.Minus(cTokenInfo.TotalCTokenMintAmount, eventDetailsEto.RedeemTokens);
            cTokenInfo.TotalUnderlyingAssetAmount =
                CalculationHelper.Minus(cTokenInfo.TotalUnderlyingAssetAmount, eventDetailsEto.RedeemAmount);
            await _cTokenRepository.UpdateAsync(cTokenInfo);
            var record = RecordGeneratorHelper.GenerateCTokenRecord(contractEventDetailsDto,
                cTokenInfo,
                eventDetailsEto.Redeemer, BehaviorType.Redeem, eventDetailsEto.RedeemAmount.ToString(),
                eventDetailsEto.RedeemTokens.ToString());
            var records = new List<CTokenRecord>
            {
                record
            };

            await _cTokenRecordRepository.InsertManyAsync(records);
        }
    }
}