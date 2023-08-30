using System.Numerics;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.BackgroundJob.Processors;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.Trade.Ethereum.Dtos;
using AwakenServer.Tokens;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Nethereum.Util;

namespace AwakenServer.ContractEventHandler.Trade.Ethereum.Processors
{
    public class SyncEventProcessor : EthereumEthereumEventProcessorBase<SyncEventDto>
    {
        private readonly IChainAppService _chainAppService;
        private readonly ITokenAppService _tokenAppService;
        private readonly ITradePairAppService _tradePairAppService;
        
        public SyncEventProcessor(ITokenAppService tokenAppService, IChainAppService chainAppService,
            ITradePairAppService tradePairAppService)
        {
            _tokenAppService = tokenAppService;
            _chainAppService = chainAppService;
            _tradePairAppService = tradePairAppService;
        }

        protected override async Task HandleEventAsync(SyncEventDto eventDetailsDto,
            ContractEventDetailsDto contractEventDetailsDto)
        {
            if (contractEventDetailsDto.StatusEnum != ContractEventStatus.Confirmed)
            {
                return;
            }
            
            var chain = await _chainAppService.GetByNameCacheAsync(contractEventDetailsDto.NodeName);
            var pool = await _tradePairAppService.GetByAddressAsync(chain.Name, contractEventDetailsDto.Address);
            var token0 = await _tokenAppService.GetAsync(pool.Token0Id);
            var token1 = await _tokenAppService.GetAsync(pool.Token1Id);

            await _tradePairAppService.UpdateLiquidityAsync(new LiquidityUpdateDto
            {
                ChainId = chain.Id,
                TradePairId = pool.Id,
                Token0Amount = ((BigDecimal)eventDetailsDto.Reserve0/ BigInteger.Pow(10, token0.Decimals)).ToString(),
                Token1Amount = ((BigDecimal)eventDetailsDto.Reserve1/ BigInteger.Pow(10, token1.Decimals)).ToString(),
                Timestamp = contractEventDetailsDto.Timestamp * 1000
            });
        }
    }
}