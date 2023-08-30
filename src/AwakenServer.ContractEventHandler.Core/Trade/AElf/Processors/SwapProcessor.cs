using System.Threading.Tasks;
using AElf;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using Awaken.Contracts.Swap;
using AwakenServer.Chains;
using AwakenServer.Tokens;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;

namespace AwakenServer.ContractEventHandler.Trade.AElf.Processors
{
    public class SwapProcessor: AElfEventProcessorBase<Swap>
    {
        private readonly IChainAppService _chainAppService;
        private readonly ITokenAppService _tokenAppService;
        private readonly ITradePairAppService _tradePairAppService;
        private readonly ITradeRecordAppService _tradeRecordAppService;

        public SwapProcessor(ITokenAppService tokenAppService, IChainAppService chainAppService,
            ITradePairAppService tradePairAppService, ITradeRecordAppService tradeRecordAppService)
        {
            _tokenAppService = tokenAppService;
            _chainAppService = chainAppService;
            _tradePairAppService = tradePairAppService;
            _tradeRecordAppService = tradeRecordAppService;
        }
        protected override async Task HandleEventAsync(Swap eventDetailsEto, EventContext txInfoDto)
        {
            var nodeName = ChainHelper.ConvertChainIdToBase58(txInfoDto.ChainId);
            var chain = await _chainAppService.GetByNameCacheAsync(nodeName);
            var pariAddress = eventDetailsEto.Pair.ToBase58();
            var pair = await _tradePairAppService.GetByAddressAsync(chain.Name, pariAddress);
            var token0 = await _tokenAppService.GetAsync(pair.Token0Id);
            var token1 = await _tokenAppService.GetAsync(pair.Token1Id);
            var sender = eventDetailsEto.Sender.ToBase58();
            var record = new TradeRecordCreateDto
            {
                ChainId = chain.Id,
                TradePairId = pair.Id,
                Address = sender,
                TransactionHash = txInfoDto.TransactionId,
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(txInfoDto.BlockTime),
                Side = token0.Symbol == eventDetailsEto.SymbolIn ? TradeSide.Sell : TradeSide.Buy,
                Channel = eventDetailsEto.Channel,
                Sender = sender
            };
            if (record.Side == TradeSide.Buy)
            {
                record.Token0Amount = eventDetailsEto.AmountOut.ToDecimalsString(token0.Decimals);
                record.Token1Amount = eventDetailsEto.AmountIn.ToDecimalsString(token1.Decimals);
            }
            else
            {
                record.Token0Amount = eventDetailsEto.AmountIn .ToDecimalsString(token0.Decimals);
                record.Token1Amount = eventDetailsEto.AmountOut.ToDecimalsString(token1.Decimals);
            }
            
            await _tradeRecordAppService.CreateAsync(record);
        }
    }
}