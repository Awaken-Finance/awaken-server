using System;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.TestBase;
using AElf.Types;
using Awaken.Contracts.Swap;
using AwakenServer.Tokens;
using Xunit;

namespace AwakenServer.Trade.AElf
{
    public class SwapEventProcessorTests: TradeProcessorTestBase
    {
        //private readonly ITradePairRepository _tradePairRepository;
        private readonly ITradePairAppService _tradePairAppService;
        private readonly ITradeRecordRepository _tradeRecordRepository;
        private readonly TokenAppService _tokenAppService;
        private readonly IEventHandlerTestProcessor<Swap> _swapEventProcessor;
        private readonly TestEnvironmentProvider _testEnvironmentProvider;

        public SwapEventProcessorTests()
        {
            _tokenAppService = GetRequiredService<TokenAppService>();
            _tradePairAppService = GetRequiredService<ITradePairAppService>();
            _tradeRecordRepository = GetRequiredService<ITradeRecordRepository>();
            _swapEventProcessor = GetRequiredService<IEventHandlerTestProcessor<Swap>>();
            _testEnvironmentProvider = GetRequiredService<TestEnvironmentProvider>();
        }

        [Fact(Skip = "no need")]
        public async Task HandleEventTest()
        {
            var pair = await _tradePairAppService.GetTradePairInfoAsync(_testEnvironmentProvider.TradePariElfUsdtId);
            var addressStr = "2EM5uV6bSJh6xJfZTUa1pZpYsYcCUAdPvZvFUJzMDJEx3rbioz";
            var tokenA = await _tokenAppService.GetAsync(pair.Token0Id);
            var tokenB = await _tokenAppService.GetAsync(pair.Token1Id);
            var swapEvent = new Swap
            {
                Sender = Address.FromBase58(addressStr),
                SymbolIn = tokenA.Symbol,
                SymbolOut = tokenB.Symbol,
                AmountIn = 100_00000000L,
                AmountOut = 1000_000000L,
                Channel = "channel",
                Pair = Address.FromBase58(pair.Address)
            }; 
            var txId = "0x4ce276db9dae6b19b97204238b";
            var returnValue = string.Empty;
            var chainId = 9992731;
            var blockNumber = 1000;
            var methodName = "liquidityRemove";
            var blockTime = DateTime.UtcNow;
            var fromAddress = "0xUser";
            var toAddress = "0xSwap";
            var blockHash = "0x0834ca06d6211906c1a2eb64a04fc1";
            var contractEvent = new EventContext
            {
                TransactionId = txId,
                Status = "MINED",
                ReturnValue = returnValue,
                ChainId = chainId,
                BlockNumber = blockNumber,
                MethodName = methodName,
                BlockTime = blockTime,
                FromAddress = fromAddress,
                ToAddress = toAddress,
                BlockHash = blockHash
            };
            await _swapEventProcessor.HandleEventAsync(swapEvent, contractEvent);

            var address2Str = "LYKSAU799wDphRK7W5ZsMBF2vDG8ijeuESk1R7Xpi6hBpdnX4";
            swapEvent = new Swap
            {
                Sender = Address.FromBase58(address2Str),
                SymbolIn = tokenB.Symbol,
                SymbolOut = tokenA.Symbol,
                AmountIn = 1000_000000L,
                AmountOut = 100_00000000L,
                Channel = "channel2",
                Pair = Address.FromBase58(pair.Address)
            }; 
            
            await _swapEventProcessor.HandleEventAsync(swapEvent, contractEvent);
        }
    }
}