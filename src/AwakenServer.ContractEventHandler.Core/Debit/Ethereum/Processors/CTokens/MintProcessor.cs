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
    public class MintProcessor : EthereumEthereumEventProcessorBase<Mint>
    {
        private readonly IChainAppService _chainAppService;
        private readonly IRepository<CToken> _cTokenRepository;
        private readonly IRepository<CTokenRecord> _cTokenRecordRepository;

        public MintProcessor(IChainAppService chainAppService, IRepository<CToken> cTokenRepository,
            IRepository<CTokenRecord> cTokenRecordRepository)
        {
            _chainAppService = chainAppService;
            _cTokenRepository = cTokenRepository;
            _cTokenRecordRepository = cTokenRecordRepository;
        }

        protected override async Task HandleEventAsync(Mint eventDetailsEto,
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
                CalculationHelper.Add(cTokenInfo.TotalCTokenMintAmount, eventDetailsEto.MintTokens);
            cTokenInfo.TotalUnderlyingAssetAmount =
                CalculationHelper.Add(cTokenInfo.TotalUnderlyingAssetAmount, eventDetailsEto.MintAmount);
            await _cTokenRepository.UpdateAsync(cTokenInfo);
            var record = RecordGeneratorHelper.GenerateCTokenRecord(contractEventDetailsDto,
                cTokenInfo,
                eventDetailsEto.Minter, BehaviorType.Mint, eventDetailsEto.MintAmount.ToString(),
                eventDetailsEto.MintTokens.ToString(), eventDetailsEto.Channel);
            var records = new List<CTokenRecord>
            {
                record
            };
            await _cTokenRecordRepository.InsertManyAsync(records);
        }
    }
}