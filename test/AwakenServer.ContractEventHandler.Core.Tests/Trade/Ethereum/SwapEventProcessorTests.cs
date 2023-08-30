using System;
using System.Numerics;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AElf.EthereumNode.EventHandler.TestBase;
using AwakenServer.ContractEventHandler.Trade.Ethereum.Dtos;
using AwakenServer.Grains.Grain.Price.TradePair;
using Shouldly;
using Xunit;

namespace AwakenServer.Trade.Ethereum
{
    public class SwapEventProcessorTests: TradeProcessorTestBase
    {
        //private readonly ITradePairRepository _tradePairRepository;
        private readonly ITradePairAppService _tradePairAppService;
        private readonly ITradeRecordRepository _tradeRecordRepository;
        private readonly IEventHandlerTestProcessor<SwapEventDto> _swapEventProcessor;
        private readonly TestEnvironmentProvider _testEnvironmentProvider;

        public SwapEventProcessorTests()
        {
            _tradePairAppService = GetRequiredService<ITradePairAppService>();
            _tradeRecordRepository = GetRequiredService<ITradeRecordRepository>();
            _swapEventProcessor = GetRequiredService<IEventHandlerTestProcessor<SwapEventDto>>();
            _testEnvironmentProvider = GetRequiredService<TestEnvironmentProvider>();
        }

        [Fact(Skip = "no need")]
        public async Task HandleEventTest()
        {
            var pair = await _tradePairAppService.GetTradePairInfoAsync(_testEnvironmentProvider.TradePairBtcEthId);
            var swapEvent = new SwapEventDto
            {
                To = "0xUserA",
                Amount0In = new BigInteger(100_00000000),
                Amount1Out = new BigInteger(1000_00000000),
                Channel = "channel",
                Sender = "sender"
            };
            var contractEvent = new ContractEventDetailsDto
            {
                Address = pair.Address,
                BlockNumber = 1000,
                StatusEnum = ContractEventStatus.Unconfirmed,
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow)/1000,
                BlockHash = "0x0834ca06d6211906c1a2eb64a04fc1",
                TransactionHash = "0x4ce276db9dae6b19b97204238b",
                NodeName = "Ethereum"
            };
            await _swapEventProcessor.HandleEventAsync(swapEvent, contractEvent);

            contractEvent.StatusEnum = ContractEventStatus.Confirmed;
            await _swapEventProcessor.HandleEventAsync(swapEvent, contractEvent);

            swapEvent = new SwapEventDto
            {
                To = "0xUserA",
                Amount0Out = new BigInteger(100_00000000),
                Amount1In = new BigInteger(1000_00000000),
                Channel = "channel2",
                Sender = "sender2"
            };
            await _swapEventProcessor.HandleEventAsync(swapEvent, contractEvent);
        }
    }
}