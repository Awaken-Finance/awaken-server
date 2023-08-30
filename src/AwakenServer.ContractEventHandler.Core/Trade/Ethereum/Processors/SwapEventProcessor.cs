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
    public class SwapEventProcessor : EthereumEthereumEventProcessorBase<SwapEventDto>
    {
        private readonly IChainAppService _chainAppService;
        private readonly ITokenAppService _tokenAppService;
        private readonly ITradePairAppService _tradePairAppService;
        private readonly ITradeRecordAppService _tradeRecordAppService;
        
        public SwapEventProcessor(ITokenAppService tokenAppService, IChainAppService chainAppService,
            ITradePairAppService tradePairAppService, ITradeRecordAppService tradeRecordAppService)
        {
            _tokenAppService = tokenAppService;
            _chainAppService = chainAppService;
            _tradePairAppService = tradePairAppService;
            _tradeRecordAppService = tradeRecordAppService;
        }

        protected override async Task HandleEventAsync(SwapEventDto eventDetailsDto,
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

            var record = new TradeRecordCreateDto
            {
                ChainId = chain.Id,
                TradePairId = pool.Id,
                Address = eventDetailsDto.To,
                TransactionHash = contractEventDetailsDto.TransactionHash,
                Timestamp = contractEventDetailsDto.Timestamp * 1000,
                Side = eventDetailsDto.Amount0In == BigInteger.Zero ? TradeSide.Buy : TradeSide.Sell,
                Channel = eventDetailsDto.Channel,
                Sender = eventDetailsDto.Sender
            };
            if (record.Side == TradeSide.Buy)
            {
                record.Token0Amount = ((BigDecimal) eventDetailsDto.Amount0Out / BigInteger.Pow(10, token0.Decimals))
                    .ToString();
                record.Token1Amount = ((BigDecimal) eventDetailsDto.Amount1In / BigInteger.Pow(10, token1.Decimals))
                    .ToString();
            }
            else
            {
                record.Token0Amount = ((BigDecimal) eventDetailsDto.Amount0In / BigInteger.Pow(10, token0.Decimals))
                    .ToString();
                record.Token1Amount = ((BigDecimal) eventDetailsDto.Amount1Out / BigInteger.Pow(10, token1.Decimals))
                    .ToString();
            }

            await _tradeRecordAppService.CreateAsync(record);
        }
    }
}