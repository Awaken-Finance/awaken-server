using System.Numerics;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.BackgroundJob.Processors;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.Price.Dtos;
using AwakenServer.Price;
using AwakenServer.Price.Dtos;
using Microsoft.Extensions.Options;
using Nethereum.Util;

namespace AwakenServer.ContractEventHandler.Price.Processors
{
    public class AnswerUpdatedEventProcessor : EthereumEthereumEventProcessorBase<AnswerUpdatedEventDto>
    {
        private readonly IChainAppService _chainAppService;
        private readonly ILendingTokenPriceAppService _lendingTokenPriceAppService;
        private readonly ChainlinkAggregatorOptions _chainlinkAggregatorOptions;
        private readonly ITokenProvider _tokenProvider;

        public AnswerUpdatedEventProcessor(IChainAppService chainAppService,
            ILendingTokenPriceAppService lendingTokenPriceAppService,
            IOptionsSnapshot<ChainlinkAggregatorOptions> chainlinkAggregatorOptions, ITokenProvider tokenProvider)
        {
            _chainAppService = chainAppService;
            _lendingTokenPriceAppService = lendingTokenPriceAppService;
            _tokenProvider = tokenProvider;
            _chainlinkAggregatorOptions = chainlinkAggregatorOptions.Value;
        }

        protected override async Task HandleEventAsync(AnswerUpdatedEventDto eventDetailsDto,
            ContractEventDetailsDto contractEventDetailsDto)
        {
            if (contractEventDetailsDto.StatusEnum != ContractEventStatus.Confirmed)
            {
                return;
            }
            var nodeName = contractEventDetailsDto.NodeName;
            var chain = await _chainAppService.GetByNameCacheAsync(nodeName);
            var aggregator = _chainlinkAggregatorOptions.Aggregators[$"{nodeName}-{contractEventDetailsDto.Address}"];
            var token = await _tokenProvider.GetOrAddTokenAsync(chain.Id, nodeName, aggregator.Token);
            var price = (BigDecimal) eventDetailsDto.Current / BigInteger.Pow(10, aggregator.Decimals);
            await _lendingTokenPriceAppService.CreateOrUpdateAsync(new LendingTokenPriceCreateOrUpdateDto
            {
                ChainId = chain.Id,
                TokenId = token.Id,
                Timestamp = eventDetailsDto.UpdatedAt * 1000,
                BlockNumber = contractEventDetailsDto.BlockNumber,
                Price = price.ToString(),
                PriceValue = (double) price
            });
        }
    }
}