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
            
            // get price update cache
            result = await _priceAppService.GetTokenPriceListAsync(new List<string> { "TESTCACHE" });
            result.Items.Count.ShouldBe(1);
            result.Items[0].PriceInUsd.ShouldBe(3.3m);
            
        }
        
        [Fact]
        public async Task PriceRelationTest()
        {
            await _tradePairMarketDataProvider.AddOrUpdateSnapshotAsync(Guid.Parse("3F2504E0-4F89-41D3-9A0C-0305E82C3301"), async grain =>
            {
                return await grain.UpdatePriceAsync(new SyncRecordGrainDto()
                {
                    ChainId = ChainName,
                    PairAddress = "0xPool006a6FaC8c710e53c4B2c2F96477119dA361",
                    Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.Now.AddDays(-3)),
                    ReserveA = NumberFormatter.WithDecimals(10, 8),
                    ReserveB = NumberFormatter.WithDecimals(90, 6),
                    BlockHeight = 101,
                    SymbolA = "CPU",
                    SymbolB = "USDT",
                    Token0PriceInUsd = 0,
                    Token1PriceInUsd = 1
                });
            });
            
            await _tradePairMarketDataProvider.AddOrUpdateSnapshotAsync(Guid.Parse("3F2504E0-4F89-41D3-9A0C-0305E82C3302"), async grain =>
            {
                return await grain.UpdatePriceAsync(new SyncRecordGrainDto()
                {
                    ChainId = ChainName,
                    PairAddress = "0xPool006a6FaC8c710e53c4B2c2F96477119dA362",
                    Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.Now.AddDays(-3)),
                    ReserveA = NumberFormatter.WithDecimals(10, 8),
                    ReserveB = NumberFormatter.WithDecimals(90, 6),
                    BlockHeight = 101,
                    SymbolA = "CPU",
                    SymbolB = "USDC",
                    Token0PriceInUsd = 0,
                    Token1PriceInUsd = 1
                });
            });
            
            await _tradePairMarketDataProvider.AddOrUpdateSnapshotAsync(Guid.Parse("3D2504E0-4F89-41D3-9A0C-0305E82C3303"), async grain =>
            {
                return await grain.UpdatePriceAsync(new SyncRecordGrainDto()
                {
                    ChainId = ChainName,
                    PairAddress = "0xPool006a6FaC8c710e53c4B2c2F96477119dA363",
                    Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.Now.AddDays(-3)),
                    ReserveA = NumberFormatter.WithDecimals(100, 8),
                    ReserveB = NumberFormatter.WithDecimals(10, 8),
                    BlockHeight = 101,
                    SymbolA = "CPU",
                    SymbolB = "READ",
                    Token0PriceInUsd = 0,
                    Token1PriceInUsd = 0
                });
            });
            
            await _priceAppService.RebuildPricingMapAsync(ChainId);
            
            var result = await _priceAppService.GetTokenPriceListAsync(new List<string> { "CPU" });
            result.Items.Count.ShouldBe(1);
            result.Items[0].PriceInUsd.ShouldBe(9.9m);
            
            result = await _priceAppService.GetTokenHistoryPriceDataAsync(new List<GetTokenHistoryPriceInput>
            {
                new GetTokenHistoryPriceInput()
                {
                    Symbol = "CPU",
                    DateTime = DateTime.Today
                }
            });
            result.Items.Count.ShouldBe(1);
            result.Items[0].PriceInUsd.ShouldBe(9.9m);
            
            result = await _priceAppService.GetTokenPriceListAsync(new List<string> { "READ" });
            result.Items.Count.ShouldBe(1);
            result.Items[0].PriceInUsd.ShouldBe(99m);
            
            // swap and update affected price
            var swapRecordDto = new SwapRecordDto
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
            await _tradeRecordAppService.CreateAsync(0,swapRecordDto);
            
            result = await _priceAppService.GetTokenPriceListAsync(new List<string> { "CPU" });
            result.Items.Count.ShouldBe(1);
            result.Items[0].PriceInUsd.ShouldBe(8.8m);
            
            result = await _priceAppService.GetTokenPriceListAsync(new List<string> { "READ" });
            result.Items.Count.ShouldBe(1);
            result.Items[0].PriceInUsd.ShouldBe(88m);
        }

        
    }
}