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
    public class SyncEventProcessorTests: TradeProcessorTestBase
    {
        private readonly ITradePairAppService _tradePairAppService;
        private readonly ITradeRecordRepository _tradeRecordRepository;
        private readonly IEventHandlerTestProcessor<SyncEventDto> _syncEventProcessor;
        private readonly TestEnvironmentProvider _testEnvironmentProvider;

        public SyncEventProcessorTests()
        {
            _tradePairAppService = GetRequiredService<ITradePairAppService>();
            _tradeRecordRepository = GetRequiredService<ITradeRecordRepository>();
            _syncEventProcessor = GetRequiredService<IEventHandlerTestProcessor<SyncEventDto>>();
            _testEnvironmentProvider = GetRequiredService<TestEnvironmentProvider>();
        }

        [Fact(Skip = "no need")]
        public async Task HandleEventTest()
        {
            var pair = await _tradePairAppService.GetTradePairInfoAsync(_testEnvironmentProvider.TradePairBtcEthId);
            var swapEvent = new SyncEventDto
            {
                Reserve0 = new BigInteger(100_00000000),
                Reserve1 = new BigInteger(1000_00000000)
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
            await _syncEventProcessor.HandleEventAsync(swapEvent, contractEvent);

            var marketDataGrain = Cluster.Client.GetGrain<ITradePairMarketDataSnapshotGrain>($"{pair.ChainId}-{pair.Id}");
            var marketDataResult = await marketDataGrain.GetAsync();
            marketDataResult.Success.ShouldBeFalse();

            contractEvent.StatusEnum = ContractEventStatus.Confirmed;
            await _syncEventProcessor.HandleEventAsync(swapEvent, contractEvent);
            
            // marketDataResult = await marketDataGrain.GetAsync();
            // var marketData = marketDataResult.Data;
            // marketData.ValueLocked0.ShouldBe(100);
            // marketData.ValueLocked1.ShouldBe(1000);
            // var time = DateTimeHelper.FromUnixTimeMilliseconds(contractEvent.Timestamp * 1000);
            // marketData.Timestamp.ShouldBe(time.Date.AddHours(time.Hour));
        }
    }
}