using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Grains.Tests;
using AwakenServer.Price.Dtos;
using AwakenServer.Trade;
using Shouldly;
using Xunit;
using TradeTokenPriceProvider = AwakenServer.Trade.ITokenPriceProvider;

namespace AwakenServer.Price
{
    [Collection(ClusterCollection.Name)]
    public sealed class MockPriceAppServiceTests : MockPriceTestBase
    {
        private readonly IPriceAppService _priceAppService;
        private readonly TradeTokenPriceProvider _tokenPriceProvider;

        public MockPriceAppServiceTests()
        {
            _priceAppService = GetRequiredService<IPriceAppService>();
            _tokenPriceProvider = GetRequiredService<TradeTokenPriceProvider>();
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
            decimal.Parse(btcPrice).ShouldBe(69000);
            var sashimiPrice = await _priceAppService.GetTokenPriceAsync(new GetTokenPriceInput
            {
                Symbol = Symbol.SASHIMI,
                ChainId = ChainId
            });
            decimal.Parse(sashimiPrice).ShouldBe(1);
            var istarPrice = await _priceAppService.GetTokenPriceAsync(new GetTokenPriceInput
            {
                Symbol = Symbol.ISTAR,
                ChainId = ChainId
            });
            decimal.Parse(istarPrice).ShouldBe(1);
            
            var ethPrice = await _priceAppService.GetTokenPriceAsync(new GetTokenPriceInput
            {
                Symbol = "ETH",
                ChainId = ChainId
            });
            decimal.Parse(ethPrice).ShouldBe(0);
            
            //Get token price from trade
            await _tokenPriceProvider.UpdatePriceAsync(ChainId, TokenBtcId, TokenUSDTId, 59366, "BTC");
            
            var newBtcPrice = await _priceAppService.GetTokenPriceAsync(new GetTokenPriceInput
            {
                TokenId = TokenBtcId,
                Symbol = Symbol.BTC,
                ChainId = ChainId
            });
            newBtcPrice.ShouldBe("69000");
            
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

            result = await _priceAppService.GetTokenPriceListAsync(new List<string> { "USDT" });
            result.Items.Count.ShouldBe(1);
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
            result.Items.Count.ShouldBe(0);
            
      
            
            result = await _priceAppService.GetTokenHistoryPriceDataAsync(new List<GetTokenHistoryPriceInput>
            {
                new GetTokenHistoryPriceInput()
                {
                    Symbol = "USDT",
                    DateTime = DateTime.UtcNow.AddDays(-1)
                } 
            });
            result.Items.Count.ShouldBe(0);
        }
    }
}