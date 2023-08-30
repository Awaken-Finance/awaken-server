using System.Numerics;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AElf.EthereumNode.EventHandler.TestBase;
using AwakenServer.ContractEventHandler.Price.Dtos;
using AwakenServer.ContractEventHandler.Price.Processors;
using Microsoft.Extensions.Options;
using Nethereum.Util;
using Shouldly;
using Xunit;

namespace AwakenServer.Price
{
    public sealed class AnswerUpdatedEventProcessorTests : PriceProcessorTestBase
    {
        private readonly ILendingTokenPriceRepository _lendingTokenPriceRepository;
        private readonly ChainlinkAggregatorOptions _chainlinkAggregatorOptions;
        private long Timestamp = 1636784478;

        public AnswerUpdatedEventProcessorTests()
        {
            _lendingTokenPriceRepository = GetRequiredService<ILendingTokenPriceRepository>();
            _chainlinkAggregatorOptions = GetRequiredService<IOptionsSnapshot<ChainlinkAggregatorOptions>>().Value;
        }
        
        [Fact(Skip = "no need")]
        public async Task AnswerUpdatedEventTest()
        {
            var answerUpdatedEventProcessor = GetRequiredService<IEventHandlerTestProcessor<AnswerUpdatedEventDto>>();
            var aggregatorAddress = "0xAggregator";
            var blockNumber = 100;
            
            var answerUpdatedEventDto = new AnswerUpdatedEventDto
            {
                Current = 600000000000000000,
                RoundId = 1,
                UpdatedAt = Timestamp
            };
            await answerUpdatedEventProcessor.HandleEventAsync(answerUpdatedEventDto,
                GetDefaultEventContext(aggregatorAddress, blockNumber:blockNumber, confirmStatus: ContractEventStatus.Unconfirmed));
            (await _lendingTokenPriceRepository.GetCountAsync()).ShouldBe(0);
            blockNumber++;
            await answerUpdatedEventProcessor.HandleEventAsync(answerUpdatedEventDto,
                GetDefaultEventContext(aggregatorAddress, blockNumber:blockNumber, confirmStatus: ContractEventStatus.Confirmed));

            var list = await _lendingTokenPriceRepository.GetListAsync();
            list.Count.ShouldBe(1);
            var price = (BigDecimal) answerUpdatedEventDto.Current /
                        BigInteger.Pow(10, _chainlinkAggregatorOptions.Aggregators[$"Ethereum-{aggregatorAddress}"].Decimals);
            list[0].Price.ShouldBe(price.ToString());
            list[0].PriceValue.ShouldBe((double) price);
            list[0].Timestamp.ShouldBe(DateTimeHelper.FromUnixTimeMilliseconds(answerUpdatedEventDto.UpdatedAt * 1000));
            list[0].ChainId.ShouldBe(ChainId);
            list[0].BlockNumber.ShouldBe(blockNumber);
        }
    }
}