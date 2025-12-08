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
    public class PairCreatedProcessorTests : TradeProcessorTestBase
    {
        //private readonly ITradePairRepository _tradePairRepository;
        private readonly ITradePairAppService _tradePairAppService;
        private readonly IEventHandlerTestProcessor<PairCreated> _pairCreatedEventProcessor;
        private readonly TestEnvironmentProvider _testEnvironmentProvider;
        private readonly ITokenAppService _tokenAppService;

        public PairCreatedProcessorTests()
        {
            _tradePairAppService = GetRequiredService<ITradePairAppService>();
            _pairCreatedEventProcessor = GetRequiredService<IEventHandlerTestProcessor<PairCreated>>();
            _testEnvironmentProvider = GetRequiredService<TestEnvironmentProvider>();
            _tokenAppService =  GetRequiredService<ITokenAppService>();
        }

        [Fact(Skip = "no need")]
        public async Task HandleEventTest()
        {
            var tokenBtc = await _tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = _testEnvironmentProvider.AElfChainId,
                Symbol = "BTC"
            });
            var tokenUsdt = await _tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = _testEnvironmentProvider.AElfChainId,
                Symbol = "USDT"
            });
            var pairCreatedEvent = new PairCreated
            {
                Pair = Address.FromBase58("2Pvmz2c57roQAJEtQ11fqavofdDtyD1Vehjxd7QRpQ7hwSqcF7"),
                SymbolA = tokenBtc.Symbol,
                SymbolB = tokenUsdt.Symbol
            };
            var txId = "0x4ce276db9dae6b19b97204238b";
            var returnValue = string.Empty;
            var chainId = 9992731;
            var blockNumber = 1000;
            var methodName = "liquidityRemove";
            var blockTime = DateTime.UtcNow;
            var fromAddress = "0xUser";
            var toAddress = "0xFactoryA";
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
            await _pairCreatedEventProcessor.HandleEventAsync(pairCreatedEvent, contractEvent);

            var pair = await _tradePairAppService.GetByAddressAsync("", pairCreatedEvent.Pair.ToBase58());
            pair.Token0Id.ShouldBe(tokenBtc.Id);
            pair.Token1Id.ShouldBe(tokenUsdt.Id);
            pair.FeeRate.ShouldBe(0.0003);
            pair.ChainId.ShouldBe(_testEnvironmentProvider.AElfChainId);
            pair.IsTokenReversed.ShouldBeFalse();
        }
        
        [Fact(Skip = "no need")]
        public async Task TokenOrderTest()
        {
            var tokenBtc = await _tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = _testEnvironmentProvider.AElfChainId,
                Symbol = "BTC"
            });
            var tokenUsdt = await _tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = _testEnvironmentProvider.AElfChainId,
                Symbol = "USDT"
            });
            var pairCreatedEvent = new PairCreated
            {
                Pair = Address.FromBase58("2Pvmz2c57roQAJEtQ11fqavofdDtyD1Vehjxd7QRpQ7hwSqcF7"),
                SymbolA = tokenUsdt.Symbol,
                SymbolB = tokenBtc.Symbol
            };
            var txId = "0x4ce276db9dae6b19b97204238b";
            var returnValue = string.Empty;
            var chainId = 9992731;
            var blockNumber = 1000;
            var methodName = "liquidityRemove";
            var blockTime = DateTime.UtcNow;
            var fromAddress = "0xUser";
            var toAddress = "0xFactoryA";
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
            await _pairCreatedEventProcessor.HandleEventAsync(pairCreatedEvent, contractEvent);

            var pair = await _tradePairAppService.GetByAddressAsync("", pairCreatedEvent.Pair.ToBase58());
            pair.Token0Id.ShouldBe(tokenBtc.Id);
            pair.Token1Id.ShouldBe(tokenUsdt.Id);
            pair.FeeRate.ShouldBe(0.0003);
            pair.ChainId.ShouldBe(_testEnvironmentProvider.AElfChainId);
            pair.IsTokenReversed.ShouldBeTrue();
        }
    }
}