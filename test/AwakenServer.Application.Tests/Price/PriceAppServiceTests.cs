using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AwakenServer.Grains.Tests;
using AwakenServer.Price.Dtos;
using AwakenServer.Tokens;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Xunit;
using TradePairIndex = AwakenServer.Trade.Index.TradePair;

namespace AwakenServer.Price
{
    [Collection(ClusterCollection.Name)]
    public sealed class PriceAppServiceTests : PriceAppServiceTestBase
    {
        private readonly IPriceAppService _priceAppService;
        private readonly TradePairTestHelper _tradePairTestHelper;
        private readonly ITokenAppService _tokenAppService;
        private readonly ITradePairMarketDataProvider _tradePairMarketDataProvider;
        private readonly ITradeRecordAppService _tradeRecordAppService;

        public PriceAppServiceTests()
        {
            _priceAppService = GetRequiredService<IPriceAppService>();
            _tradePairTestHelper = GetRequiredService<TradePairTestHelper>();
            _tokenAppService = GetRequiredService<ITokenAppService>();
            _tradePairMarketDataProvider = GetRequiredService<ITradePairMarketDataProvider>();
            _tradeRecordAppService = GetRequiredService<ITradeRecordAppService>();
        }

        [Fact]
        public async Task GetTokenPriceListTest()
        {
            var result = await _priceAppService.GetTokenPriceListAsync(new List<string> { "USDT" });
            result.Items.Count.ShouldBe(1);
            result.Items[0].PriceInUsd.ShouldBe(1.1m);
            
            result = await _priceAppService.GetTokenPriceListAsync(new List<string> { "ETH" });
            result.Items.Count.ShouldBe(1);
            result.Items[0].PriceInUsd.ShouldBe(1.2m);
            
            result = await _priceAppService.GetTokenPriceListAsync(new List<string> { "SGR-1" });
            result.Items.Count.ShouldBe(1);
            result.Items[0].PriceInUsd.ShouldBe(2.2m);
            
            result = await _priceAppService.GetTokenPriceListAsync(new List<string> { "XXX-1" });
            result.Items.Count.ShouldBe(1);
            result.Items[0].PriceInUsd.ShouldBe(0);
        }
        
        [Fact]
        public async Task CacheTest()
        {
            // make cache
            var result = await _priceAppService.GetTokenPriceListAsync(new List<string> { "TESTCACHE" });
            result.Items.Count.ShouldBe(1);
            result.Items[0].PriceInUsd.ShouldBe(1.3m);
            
            // make cache expiration
            Thread.Sleep(3000);
            
            // can't get price from API, return the last price
            result = await _priceAppService.GetTokenPriceListAsync(new List<string> { "TESTCACHE" });
            result.Items.Count.ShouldBe(1);
            result.Items[0].PriceInUsd.ShouldBe(1.3m);
            
            // get price update cache no request to api in 3 minutes
            result = await _priceAppService.GetTokenPriceListAsync(new List<string> { "TESTCACHE" });
            result.Items.Count.ShouldBe(1);
            result.Items[0].PriceInUsd.ShouldBe(1.3m);
            
            Thread.Sleep(180001);
            
            result = await _priceAppService.GetTokenPriceListAsync(new List<string> { "TESTCACHE" });
            result.Items.Count.ShouldBe(1);
            result.Items[0].PriceInUsd.ShouldBe(3.3m);
        }
        
        
        public async Task BuildAndSwapTest(int index)
        {

            await _priceAppService.RebuildPricingMapAsync(ChainId);
            
            var result = await _priceAppService.GetTokenPriceListAsync(new List<string> { "CPU", "USDT", "ETH", "SHIWN-1", "SHIWN-88", "READ" });
            result.Items.Count.ShouldBe(6);
            result.Items[0].PriceInUsd.ShouldBe(9.9m);
            result.Items[1].PriceInUsd.ShouldBe(1.1m);
            result.Items[2].PriceInUsd.ShouldBe(1.2m);
            result.Items[3].PriceInUsd.ShouldBe(3.09375m);
            result.Items[4].PriceInUsd.ShouldBe(5.15625m);
            result.Items[5].PriceInUsd.ShouldBe(99m);
            
            result = await _priceAppService.GetTokenHistoryPriceDataAsync(new List<GetTokenHistoryPriceInput>
            {
                new GetTokenHistoryPriceInput()
                {
                    Symbol = "CPU",
                    DateTime = DateTime.Today
                },
                new GetTokenHistoryPriceInput()
                {
                    Symbol = "SHIWN-88",
                    DateTime = DateTime.Today
                }
            });
            result.Items.Count.ShouldBe(2);
            result.Items[0].PriceInUsd.ShouldBe(9.9m);
            result.Items[1].PriceInUsd.ShouldBe(5.15625m);

            var txnHash = $"6622966a928185655d691565d6128835e7d1ccdf1dd3b5f277c5f2a5b2802d3{index}";
            // swap and update affected price
            var swapRecordDto = new SwapRecordDto
            {
                ChainId = ChainName,
                PairAddress = "0xPool006a6FaC8c710e53c4B2c2F96477119dA361",
                Sender = "TV2aRV4W5oSJzxrkBvj8XmJKkMCiEQnAvLmtM9BqLTN3beXm2",
                TransactionHash = txnHash,
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow),
                AmountOut = NumberFormatter.WithDecimals(8, 6),
                AmountIn = NumberFormatter.WithDecimals(1, 8),
                SymbolOut = "USDT",
                SymbolIn = "CPU",
                Channel = "test",
                BlockHeight = 99
            };
            await _tradeRecordAppService.CreateAsync(swapRecordDto);
            
            result = await _priceAppService.GetTokenPriceListAsync(new List<string> { "CPU", "USDT", "ETH", "READ", "SHIWN-1", "SHIWN-88" });
            result.Items.Count.ShouldBe(6);
            result.Items[0].PriceInUsd.ShouldBe(8.8m);
            result.Items[1].PriceInUsd.ShouldBe(1.1m);
            result.Items[2].PriceInUsd.ShouldBe(1.2m);
            result.Items[3].PriceInUsd.ShouldBe(88m);
            result.Items[4].PriceInUsd.ShouldBe(2.75m);
            result.Items[5].PriceInUsd.ShouldBe(4.58333333333333m);
            
            swapRecordDto = new SwapRecordDto
            {
                ChainId = ChainName,
                PairAddress = "0xPool006a6FaC8c710e53c4B2c2F96477119dA361",
                Sender = "TV2aRV4W5oSJzxrkBvj8XmJKkMCiEQnAvLmtM9BqLTN3beXm2",
                TransactionHash = "6622966a928185655d691565d6128835e7d1ccdf1dd3b5f277c5f2a5b2802d37",
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow),
                AmountOut = NumberFormatter.WithDecimals(8, 6),
                AmountIn = NumberFormatter.WithDecimals(1, 8),
                SymbolOut = "USDT",
                SymbolIn = "CPU",
                Channel = "test",
                BlockHeight = 99
            };
            await _tradeRecordAppService.CreateAsync(swapRecordDto);
        }

        [Fact]
        public async Task PricingRoute()
        {
            int epoch = 5;
            for (int i = 0; i < epoch; i++)
            {
                await BuildAndSwapTest(i);
            }
        }

        
    }
}