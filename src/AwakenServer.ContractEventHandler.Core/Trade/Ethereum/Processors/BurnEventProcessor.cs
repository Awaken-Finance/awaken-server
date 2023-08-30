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
    public class BurnEventProcessor : EthereumEthereumEventProcessorBase<BurnEventDto>
    {
        private readonly IChainAppService _chainAppService;
        private readonly ITokenAppService _tokenAppService;
        private readonly ITradePairAppService _tradePairAppService;
        private readonly ILiquidityAppService _liquidityAppService;

        public BurnEventProcessor(ITokenAppService tokenAppService, IChainAppService chainAppService,
            ITradePairAppService tradePairAppService, ILiquidityAppService liquidityAppService)
        {
            _tokenAppService = tokenAppService;
            _chainAppService = chainAppService;
            _tradePairAppService = tradePairAppService;
            _liquidityAppService = liquidityAppService;
        }

        protected override async Task HandleEventAsync(BurnEventDto eventDetailsDto,
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

            await _liquidityAppService.CreateAsync(new LiquidityRecordCreateDto
            {
                ChainId = chain.Id,
                TradePairId = pool.Id,
                Address = eventDetailsDto.To,
                TransactionHash = contractEventDetailsDto.TransactionHash,
                Timestamp = contractEventDetailsDto.Timestamp * 1000,
                Type = LiquidityType.Burn,
                Token0Amount = ((BigDecimal)eventDetailsDto.Amount0/ BigInteger.Pow(10, token0.Decimals)).ToString(),
                Token1Amount = ((BigDecimal)eventDetailsDto.Amount1/ BigInteger.Pow(10, token1.Decimals)).ToString(),
                LpTokenAmount = ((BigDecimal)eventDetailsDto.Liquidity/ BigInteger.Pow(10, 18)).ToString()
            });
        }
    }
}