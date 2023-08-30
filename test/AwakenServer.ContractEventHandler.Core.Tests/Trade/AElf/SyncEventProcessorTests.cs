using System;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.TestBase;
using AElf.Types;
using Awaken.Contracts.Swap;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Tokens;
using Shouldly;
using Xunit;

namespace AwakenServer.Trade.AElf
{
    public class SyncEventProcessorTests: TradeProcessorTestBase
    {
        //private readonly ITradePairRepository _tradePairRepository;
        private readonly ITradePairAppService _tradePairAppService;
        private readonly IEventHandlerTestProcessor<Sync> _syncEventProcessor;
        private readonly TestEnvironmentProvider _testEnvironmentProvider;
        private readonly ITokenAppService _tokenAppService;

        public SyncEventProcessorTests()
        {
            _tradePairAppService = GetRequiredService<ITradePairAppService>();
            _syncEventProcessor = GetRequiredService<IEventHandlerTestProcessor<Sync>>();
            _testEnvironmentProvider = GetRequiredService<TestEnvironmentProvider>();
            _tokenAppService = GetRequiredService<ITokenAppService>();
        }

        [Fact(Skip = "no need")]
        public async Task HandleEventTest()
        {
            var pair = await _tradePairAppService.GetTradePairInfoAsync(_testEnvironmentProvider.TradePariElfUsdtId);
            var token0 = await _tokenAppService.GetAsync(pair.Token0Id);
            var token1 = await _tokenAppService.GetAsync(pair.Token1Id);
            var swapEvent = new Sync
            {
                Pair = Address.FromBase58(pair.Address),
                ReserveA = 100_00000000L,
                ReserveB = 1000_000000L,
                SymbolA = token0.Symbol,
                SymbolB = token1.Symbol
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
            await _syncEventProcessor.HandleEventAsync(swapEvent, contractEvent);

            // var time = DateTimeHelper.ToUnixTimeMilliseconds(contractEvent.BlockTime.Date.AddHours(contractEvent.BlockTime.Hour));
            // var marketDataGrain = Cluster.Client.GetGrain<ITradePairMarketDataSnapshotGrain>($"{pair.ChainId}-{pair.Id}-{time}");
            // var marketDataResult = await marketDataGrain.GetAsync();
            // var marketData = marketDataResult.Data;
            // marketData.ValueLocked0.ShouldBe(100);
            // marketData.ValueLocked1.ShouldBe(1000);
            // marketData.Timestamp.ShouldBe(contractEvent.BlockTime.Date.AddHours(contractEvent.BlockTime.Hour));
        }
        
        [Fact(Skip = "no need")]
        public async Task HandleEvent_Reversed_Test()
        {
            var pair = await _tradePairAppService.GetTradePairInfoAsync(_testEnvironmentProvider.TradePariElfUsdtId);
            var token0 = await _tokenAppService.GetAsync(pair.Token0Id);
            var token1 = await _tokenAppService.GetAsync(pair.Token1Id);
            var swapEvent = new Sync
            {
                Pair = Address.FromBase58(pair.Address),
                ReserveA = 1000_000000L,
                ReserveB = 100_00000000L,
                SymbolA = token1.Symbol,
                SymbolB = token0.Symbol
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
            await _syncEventProcessor.HandleEventAsync(swapEvent, contractEvent);

            // var time = DateTimeHelper.ToUnixTimeMilliseconds(contractEvent.BlockTime.Date.AddHours(contractEvent.BlockTime.Hour));
            // var marketDataGrain = Cluster.Client.GetGrain<ITradePairMarketDataSnapshotGrain>($"{pair.ChainId}-{pair.Id}-{time}");
            // var marketDataResult = await marketDataGrain.GetAsync();
            // var marketData = marketDataResult.Data;
            // marketData.ValueLocked0.ShouldBe(100);
            // marketData.ValueLocked1.ShouldBe(1000);
            // marketData.Timestamp.ShouldBe(contractEvent.BlockTime.Date.AddHours(contractEvent.BlockTime.Hour));
        }
    }
}