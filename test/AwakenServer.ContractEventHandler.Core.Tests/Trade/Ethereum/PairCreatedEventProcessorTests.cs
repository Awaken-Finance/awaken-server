using System;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AElf.EthereumNode.EventHandler.TestBase;
using AwakenServer.ContractEventHandler.Trade.Ethereum.Dtos;
using AwakenServer.Tokens;
using AwakenServer.Trade.Dtos;
using Shouldly;
using Xunit;

namespace AwakenServer.Trade.Ethereum
{
    public class PairCreatedEventProcessorTests : TradeProcessorTestBase
    {
        //private readonly ITradePairRepository _tradePairRepository;
        private readonly ITradePairAppService _tradePairAppService;
        private readonly IEventHandlerTestProcessor<PairCreatedEventDto> _pairCreatedEventProcessor;
        private readonly TestEnvironmentProvider _testEnvironmentProvider;
        private readonly TokenAppService _tokenAppService;

        public PairCreatedEventProcessorTests()
        {
            _tradePairAppService = GetRequiredService<ITradePairAppService>();
            _pairCreatedEventProcessor = GetRequiredService<IEventHandlerTestProcessor<PairCreatedEventDto>>();
            _testEnvironmentProvider = GetRequiredService<TestEnvironmentProvider>();
            _tokenAppService = GetRequiredService<TokenAppService>();
        }

        [Fact(Skip = "no need")]
        public async Task HandleEventTest()
        {
            var tokenBtc = await _tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = _testEnvironmentProvider.EthChainId,
                Symbol = "BTC"
            });
            var tokenUsdt = await _tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = _testEnvironmentProvider.EthChainId,
                Symbol = "USDT"
            });
            var pairCreatedEvent = new PairCreatedEventDto
            {
                Count = 1,
                Pair = "0xPairA",
                Token0 = tokenBtc.Address,
                Token1 = tokenUsdt.Address
            };
            var contractEvent = new ContractEventDetailsDto
            {
                Address = "0xFactoryA",
                BlockNumber = 1000,
                StatusEnum = ContractEventStatus.Unconfirmed,
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow),
                BlockHash = "0x0834ca06d6211906c1a2eb64a04fc1",
                TransactionHash = "0x4ce276db9dae6b19b97204238b",
                NodeName = "Ethereum"
            };
            await _pairCreatedEventProcessor.HandleEventAsync(pairCreatedEvent, contractEvent);

            /*var pairs = await _tradePairAppService.GetTradePairInfoListAsync(new GetTradePairsInfoInput());
            pairs.Count.ShouldBe(2);

            contractEvent.StatusEnum = ContractEventStatus.Confirmed;
            await _pairCreatedEventProcessor.HandleEventAsync(pairCreatedEvent, contractEvent);
            
            var pair = await _tradePairAppService.GetByAddressAsync("",pairCreatedEvent.Pair);
            pair.Token0Id.ShouldBe(tokenBtc.Id);
            pair.Token1Id.ShouldBe(tokenUsdt.Id);
            pair.FeeRate.ShouldBe(0.0003);
            pair.ChainId.ShouldBe(_testEnvironmentProvider.EthChainId);*/
        }
    }
}