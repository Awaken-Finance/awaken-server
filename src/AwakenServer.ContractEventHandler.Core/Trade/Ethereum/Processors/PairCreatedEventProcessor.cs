using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.BackgroundJob.Processors;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.Trade.Ethereum.Dtos;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Microsoft.Extensions.Options;

namespace AwakenServer.ContractEventHandler.Trade.Ethereum.Processors
{
    public class PairCreatedEventProcessor : EthereumEthereumEventProcessorBase<PairCreatedEventDto>
    {
        private readonly IChainAppService _chainAppService;
        private readonly ITradePairAppService _tradePairAppService;
        private readonly FactoryContractOptions _factoryContractOptions;
        private readonly IPairEventSubscribeProvider _pairEventSubscribeProvider;
        private readonly ITokenProvider _tokenProvider;

        public PairCreatedEventProcessor(ITokenProvider tokenProvider, IChainAppService chainAppService,
            ITradePairAppService tradePairAppService, IOptionsSnapshot<FactoryContractOptions> pairFeeRateOptions,
            IOptionsSnapshot<ApiOptions> apiOptions,
            IPairEventSubscribeProvider pairEventSubscribeProvider)
        {
            _tokenProvider = tokenProvider;
            _chainAppService = chainAppService;
            _tradePairAppService = tradePairAppService;
            _pairEventSubscribeProvider = pairEventSubscribeProvider;
            _factoryContractOptions = pairFeeRateOptions.Value;
        }

        protected override async Task HandleEventAsync(PairCreatedEventDto eventDetailsDto,
            ContractEventDetailsDto contractEventDetailsDto)
        {
            if (contractEventDetailsDto.StatusEnum != ContractEventStatus.Confirmed)
            {
                return;
            }
            
            var chain = await _chainAppService.GetByNameCacheAsync(contractEventDetailsDto.NodeName);
            var token0 = await _tokenProvider.GetOrAddTokenAsync(chain.Id,chain.Name,eventDetailsDto.Token0);
            var token1 = await _tokenProvider.GetOrAddTokenAsync(chain.Id,chain.Name,eventDetailsDto.Token1);
            
            await _tradePairAppService.CreateAsync(new TradePairCreateDto
            {
                ChainId = chain.Id,
                Address = eventDetailsDto.Pair,
                Token0Id = token0.Id,
                Token1Id = token1.Id,
                FeeRate = 
                     _factoryContractOptions.Contracts[contractEventDetailsDto.Address]
            });

            await _pairEventSubscribeProvider.SubscribeEventAsync(contractEventDetailsDto.NodeName,(long) contractEventDetailsDto.BlockNumber,
                eventDetailsDto.Pair);
        }
    }
}