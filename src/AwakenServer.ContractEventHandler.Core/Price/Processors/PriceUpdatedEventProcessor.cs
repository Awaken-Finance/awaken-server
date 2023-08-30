using System.Numerics;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.BackgroundJob.Processors;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.Price.Dtos;
using AwakenServer.Price;
using AwakenServer.Price.Dtos;
using Nethereum.Util;

namespace AwakenServer.ContractEventHandler.Price.Processors
{
    public class PriceUpdatedEventProcessor : EthereumEthereumEventProcessorBase<PriceUpdatedEventDto>
    {
        private readonly IChainAppService _chainAppService;
        private readonly ILendingTokenPriceAppService _lendingTokenPriceAppService;
        private readonly ITokenProvider _tokenProvider;

        public PriceUpdatedEventProcessor(IChainAppService chainAppService,
            ILendingTokenPriceAppService lendingTokenPriceAppService, ITokenProvider tokenProvider)
        {
            _chainAppService = chainAppService;
            _lendingTokenPriceAppService = lendingTokenPriceAppService;
            _tokenProvider = tokenProvider;
        }

        protected override async Task HandleEventAsync(PriceUpdatedEventDto eventDetailsDto,
            ContractEventDetailsDto contractEventDetailsDto)
        {
            if (contractEventDetailsDto.StatusEnum != ContractEventStatus.Confirmed)
            {
                return;
            }
            var nodeName = contractEventDetailsDto.NodeName;
            var chain = await _chainAppService.GetByNameCacheAsync(nodeName);
            var token = await _tokenProvider.GetOrAddTokenAsync(chain.Id, nodeName, eventDetailsDto.Underlying);
            var price = (BigDecimal) eventDetailsDto.Price / BigInteger.Pow(10, 6);
            await _lendingTokenPriceAppService.CreateOrUpdateAsync(new LendingTokenPriceCreateOrUpdateDto
            {
                ChainId = chain.Id,
                TokenId = token.Id,
                Timestamp = eventDetailsDto.Timestamp * 1000,
                BlockNumber = (long) contractEventDetailsDto.BlockNumber,
                Price = price.ToString(),
                PriceValue = (double) price
            });
        }
    }
}