using System;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.TestBase;
using AElf.Types;
using Awaken.Contracts.Swap;
using AwakenServer.Tokens;
using Shouldly;
using Xunit;

namespace AwakenServer.Trade.AElf
{
    public class LiquidityRemovedProcessorTests : TradeProcessorTestBase
    {
        //private readonly ITradePairRepository _tradePairRepository;
        private readonly ITradePairAppService _tradePairAppService;
        private readonly ILiquidityRecordRepository _liquidityRecordRepository;
        private readonly IEventHandlerTestProcessor<LiquidityRemoved> _burnEventProcessor;
        private readonly TestEnvironmentProvider _testEnvironmentProvider;
        private readonly ITokenAppService _tokenAppService;

        public LiquidityRemovedProcessorTests()
        {
            _tradePairAppService = GetRequiredService<ITradePairAppService>();
            _liquidityRecordRepository = GetRequiredService<ILiquidityRecordRepository>();
            _burnEventProcessor = GetRequiredService<IEventHandlerTestProcessor<LiquidityRemoved>>();
            _testEnvironmentProvider = GetRequiredService<TestEnvironmentProvider>();
            _tokenAppService = GetRequiredService<ITokenAppService>();
        }

        [Fact(Skip = "no need")]
        public async Task HandleEventTest()
        {
            var pair = await _tradePairAppService.GetTradePairInfoAsync(_testEnvironmentProvider.TradePariElfUsdtId);
            var token0 = await _tokenAppService.GetAsync(pair.Token0Id);
            var token1 = await _tokenAppService.GetAsync(pair.Token1Id);
            var swapEvent = new LiquidityRemoved
            {
                Pair = Address.FromBase58(pair.Address),
                AmountA = 100_00000000L,
                AmountB = 1000_000000L,
                SymbolA = token0.Symbol,
                SymbolB = token1.Symbol,
                LiquidityToken = 2000_00000000L,
                Sender = Address.FromBase58("2EM5uV6bSJh6xJfZTUa1pZpYsYcCUAdPvZvFUJzMDJEx3rbioz")
            };
            var txId = "liquidityRemoved";
            var returnValue = string.Empty;
            var chainId = 9992731;
            var blockNumber = 1000;
            var methodName = "liquidityRemove";
            var blockTime = DateTime.UtcNow;
            var fromAddress = "0xUser";
            var toAddress = "0xSwap";
            var blockHash = "burn";
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
            await _burnEventProcessor.HandleEventAsync(swapEvent, contractEvent);
            var records = await _liquidityRecordRepository.GetListAsync();
            records.Count.ShouldBe(1);
            records[0].TradePairId.ShouldBe(pair.Id);
            records[0].Address.ShouldBe(swapEvent.Sender.ToBase58());
            records[0].Type.ShouldBe(LiquidityType.Burn);
            records[0].Token0Amount.ShouldBe("100");
            records[0].Token1Amount.ShouldBe("1000");
            records[0].LpTokenAmount.ShouldBe("2000");
            records[0].Timestamp
                .ShouldBe(DateTimeHelper.FromUnixTimeMilliseconds(DateTimeHelper.ToUnixTimeMilliseconds(blockTime)));
            records[0].TransactionHash.ShouldBe(txId);
        }
        
        [Fact(Skip = "no need")]
        public async Task HandleEvent_Reversed_Test()
        {
            var pair = await _tradePairAppService.GetTradePairInfoAsync(_testEnvironmentProvider.TradePariElfUsdtId);
            var token0 = await _tokenAppService.GetAsync(pair.Token0Id);
            var token1 = await _tokenAppService.GetAsync(pair.Token1Id);
            var swapEvent = new LiquidityRemoved
            {
                Pair = Address.FromBase58(pair.Address),
                AmountA = 1000_000000L,
                AmountB = 100_00000000L,
                SymbolA = token1.Symbol,
                SymbolB = token0.Symbol,
                LiquidityToken = 2000_00000000L,
                Sender = Address.FromBase58("2EM5uV6bSJh6xJfZTUa1pZpYsYcCUAdPvZvFUJzMDJEx3rbioz")
            };
            var txId = "liquidityRemoved";
            var returnValue = string.Empty;
            var chainId = 9992731;
            var blockNumber = 1000;
            var methodName = "liquidityRemove";
            var blockTime = DateTime.UtcNow;
            var fromAddress = "0xUser";
            var toAddress = "0xSwap";
            var blockHash = "burn";
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
            await _burnEventProcessor.HandleEventAsync(swapEvent, contractEvent);
            var records = await _liquidityRecordRepository.GetListAsync();
            records.Count.ShouldBe(1);
            records[0].TradePairId.ShouldBe(pair.Id);
            records[0].Address.ShouldBe(swapEvent.Sender.ToBase58());
            records[0].Type.ShouldBe(LiquidityType.Burn);
            records[0].Token0Amount.ShouldBe("100");
            records[0].Token1Amount.ShouldBe("1000");
            records[0].LpTokenAmount.ShouldBe("2000");
            records[0].Timestamp
                .ShouldBe(DateTimeHelper.FromUnixTimeMilliseconds(DateTimeHelper.ToUnixTimeMilliseconds(blockTime)));
            records[0].TransactionHash.ShouldBe(txId);
        }
    }
}