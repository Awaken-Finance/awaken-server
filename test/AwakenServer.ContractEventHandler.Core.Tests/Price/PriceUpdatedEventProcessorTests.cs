using System.Numerics;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AElf.EthereumNode.EventHandler.TestBase;
using AwakenServer.ContractEventHandler.Price.Dtos;
using Nethereum.Util;
using Shouldly;
using Xunit;

namespace AwakenServer.Price
{
    public sealed class PriceUpdatedEventProcessorTests : PriceProcessorTestBase
    {
        private readonly ILendingTokenPriceRepository _lendingTokenPriceRepository;
        private long Timestamp = 1636784478;

        public PriceUpdatedEventProcessorTests()
        {
            _lendingTokenPriceRepository = GetRequiredService<ILendingTokenPriceRepository>();
        }
        
        [Fact(Skip = "no need")]
        public async Task PriceUpdatedEventTest()
        {
            var priceUpdatedEventProcessor = GetRequiredService<IEventHandlerTestProcessor<PriceUpdatedEventDto>>();
            var blockNumber = 100;
            var priceOracle = "0xSimplePriceOracle";
            
            var priceUpdatedEventDto = new PriceUpdatedEventDto
            {
                Price = 60000000,
                Symbol = "ETH",
                Timestamp = Timestamp,
                Underlying = "0xETH"
            };
            await priceUpdatedEventProcessor.HandleEventAsync(priceUpdatedEventDto,
                GetDefaultEventContext(priceOracle, blockNumber:blockNumber, confirmStatus: ContractEventStatus.Unconfirmed));
            (await _lendingTokenPriceRepository.GetCountAsync()).ShouldBe(0);
            blockNumber++;
            await priceUpdatedEventProcessor.HandleEventAsync(priceUpdatedEventDto,
                GetDefaultEventContext(priceOracle, blockNumber:blockNumber, confirmStatus: ContractEventStatus.Confirmed));

            var list = await _lendingTokenPriceRepository.GetListAsync();
            list.Count.ShouldBe(1);
            var price = (BigDecimal) priceUpdatedEventDto.Price / BigInteger.Pow(10, 6);
            list[0].Price.ShouldBe(price.ToString());
            list[0].PriceValue.ShouldBe((double) price);
            list[0].Timestamp.ShouldBe(DateTimeHelper.FromUnixTimeMilliseconds(priceUpdatedEventDto.Timestamp * 1000));
            list[0].ChainId.ShouldBe(ChainId);
            list[0].BlockNumber.ShouldBe(blockNumber);
        }
    }
}