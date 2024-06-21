using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AwakenServer.Grains.Tests;
using AwakenServer.Price.Dtos;
using AwakenServer.Trade;
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

        public PriceAppServiceTests()
        {
            _priceAppService = GetRequiredService<IPriceAppService>();
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
            
            // get price update cache
            result = await _priceAppService.GetTokenPriceListAsync(new List<string> { "TESTCACHE" });
            result.Items.Count.ShouldBe(1);
            result.Items[0].PriceInUsd.ShouldBe(3.3m);
            
        }
        
        [Fact]
        public async Task PriceRelationTest()
        {
            //todo
        }

        
    }
}