using System;
using System.Numerics;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AElf.EthereumNode.EventHandler.TestBase;
using AwakenServer.ContractEventHandler.Trade.Ethereum.Dtos;
using Shouldly;
using Xunit;

namespace AwakenServer.Trade.Ethereum
{
    public class MintEventProcessorTests : TradeProcessorTestBase
    {
        //private readonly ITradePairRepository _tradePairRepository;
        private readonly ITradePairAppService _tradePairAppService;
        private readonly ILiquidityRecordRepository _liquidityRecordRepository;
        private readonly IEventHandlerTestProcessor<MintEventDto> _mintEventProcessor;
        private readonly TestEnvironmentProvider _testEnvironmentProvider;

        public MintEventProcessorTests()
        {
            _tradePairAppService = GetRequiredService<ITradePairAppService>();
            _liquidityRecordRepository = GetRequiredService<ILiquidityRecordRepository>();
            _mintEventProcessor = GetRequiredService<IEventHandlerTestProcessor<MintEventDto>>();
            _testEnvironmentProvider = GetRequiredService<TestEnvironmentProvider>();
        }

        [Fact(Skip = "no use")]
        public async Task HandleEventTest()
        {
            var pair = await _tradePairAppService.GetTradePairInfoAsync(_testEnvironmentProvider.TradePairBtcEthId);
            var mintEvent = new MintEventDto
            {
                To = "0xUserA",
                Amount0 = new BigInteger(100_00000000),
                Amount1 = new BigInteger(1000_00000000),
                Liquidity = new BigInteger(2000_000000000000000000d),
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
            await _mintEventProcessor.HandleEventAsync(mintEvent, contractEvent);
            
            var records = await _liquidityRecordRepository.GetListAsync();
            records.Count.ShouldBe(0);

            contractEvent.StatusEnum = ContractEventStatus.Confirmed;
            await _mintEventProcessor.HandleEventAsync(mintEvent, contractEvent);
            
            records = await _liquidityRecordRepository.GetListAsync();
            records.Count.ShouldBe(1);
            records[0].TradePairId.ShouldBe(pair.Id);
            records[0].Address.ShouldBe(mintEvent.To);
            records[0].Type.ShouldBe(LiquidityType.Mint);
            records[0].Token0Amount.ShouldBe("100");
            records[0].Token1Amount.ShouldBe("1000");
            records[0].LpTokenAmount.ShouldBe("2000");
            records[0].Timestamp.ShouldBe(DateTimeHelper.FromUnixTimeMilliseconds(contractEvent.Timestamp * 1000));
            records[0].TransactionHash.ShouldBe(contractEvent.TransactionHash);
            records[0].Channel.ShouldBe(mintEvent.Channel);
            records[0].Sender.ShouldBe(mintEvent.Sender);
        }
    }
}