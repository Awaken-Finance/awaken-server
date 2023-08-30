using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Price.Dtos;
using AwakenServer.Trade;
using Shouldly;
using Xunit;

namespace AwakenServer.Price
{
    public sealed class PriceAppServiceTests : PriceTestBase
    {
        private readonly IPriceAppService _priceAppService;
        private readonly ITokenPriceProvider _tokenPriceProvider;

        public PriceAppServiceTests()
        {
            _priceAppService = GetRequiredService<IPriceAppService>();
            _tokenPriceProvider = GetRequiredService<ITokenPriceProvider>();
        }

        [Fact]
        public async Task GetTokenPriceTest()
        {
            //Get token price from price provider
            var btcPrice = await _priceAppService.GetTokenPriceAsync(new GetTokenPriceInput
            {
                Symbol = Symbol.BTC,
                ChainId = ChainId
            });
            decimal.Parse(btcPrice).ShouldBe(0);
            var sashimiPrice = await _priceAppService.GetTokenPriceAsync(new GetTokenPriceInput
            {
                Symbol = Symbol.SASHIMI,
                ChainId = ChainId
            });
            decimal.Parse(sashimiPrice).ShouldBe(0);
            var istarPrice = await _priceAppService.GetTokenPriceAsync(new GetTokenPriceInput
            {
                Symbol = Symbol.ISTAR,
                ChainId = ChainId
            });
            decimal.Parse(istarPrice).ShouldBe(0);
            
            var ethPrice = await _priceAppService.GetTokenPriceAsync(new GetTokenPriceInput
            {
                Symbol = "ETH",
                ChainId = ChainId
            });
            decimal.Parse(ethPrice).ShouldBe(0);
            
            //Get token price from trade
            await _tokenPriceProvider.UpdatePriceAsync(ChainId, TokenBtcId, TokenUSDTId, 59366);
            
            var newBtcPrice = await _priceAppService.GetTokenPriceAsync(new GetTokenPriceInput
            {
                TokenId = TokenBtcId,
                Symbol = Symbol.BTC,
                ChainId = ChainId
            });
            newBtcPrice.ShouldBe("0");
            
            newBtcPrice = await _priceAppService.GetTokenPriceAsync(new GetTokenPriceInput
            {
                TokenAddress = TokenBtc.Address,
                ChainId = ChainId
            });
            newBtcPrice.ShouldBe("0");
            
            
            var noPrice = await _priceAppService.GetTokenPriceAsync(new GetTokenPriceInput
            {
                TokenAddress = "0xNull",
                ChainId = ChainId
            });
            noPrice.ShouldBe("0");
            
            ethPrice = await _priceAppService.GetTokenPriceAsync(new GetTokenPriceInput
            {
                TokenAddress = TokenEth.Address,
                ChainId = ChainId
            });
            ethPrice.ShouldBe("0");
            
            sashimiPrice = await _priceAppService.GetTokenPriceAsync(new GetTokenPriceInput
            {
                TokenAddress = TokenSashimi.Address,
                ChainId = ChainId
            });
            decimal.Parse(sashimiPrice).ShouldBe(0);
        }

        [Fact]
        public async Task GetTokenPriceListTest()
        {
            var result = await _priceAppService.GetTokenPriceListAsync(new List<string> { });
            result.Items.Count.ShouldBe(0);

            result = await _priceAppService.GetTokenPriceListAsync(new List<string> { "ELF" });
            result.Items.Count.ShouldBe(1);
            //result.Items[0].PriceInUsd.ShouldBe(123);
        }
        
        [Fact]
        public async Task GetTokenHistoryPriceDataAsyncTest()
        {
            var result = await _priceAppService.GetTokenHistoryPriceDataAsync(new List<GetTokenHistoryPriceInput>
            {
                new GetTokenHistoryPriceInput()
                {
                    DateTime = DateTime.UtcNow.AddDays(-1)
                } 
            });
            result.Items.Count.ShouldBe(1);
            
            var exception = await Assert.ThrowsAsync<NullReferenceException>(async () =>
            {
                await _priceAppService.GetTokenHistoryPriceDataAsync(new List<GetTokenHistoryPriceInput>{ null });
            });
            exception.Message.ShouldContain("Object reference not set to an instance of an object");
            
            result = await _priceAppService.GetTokenHistoryPriceDataAsync(new List<GetTokenHistoryPriceInput>
            {
                new GetTokenHistoryPriceInput()
                {
                    Symbol = "ELF",
                    DateTime = DateTime.UtcNow.AddDays(-1)
                } 
            });
            result.Items.Count.ShouldBe(1);
            //result.Items[0].PriceInUsd.ShouldBe(123);
        }
    }
}