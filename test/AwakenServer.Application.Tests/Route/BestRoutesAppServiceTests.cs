using AwakenServer.Grains.Tests;
using AwakenServer.Trade;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Grains.Tests;
using AwakenServer.Route.Dtos;
using AwakenServer.SwapTokenPath;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Org.BouncyCastle.Crypto.Prng.Drbg;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Threading;
using Volo.Abp.Validation;
using Xunit;

namespace AwakenServer.Route
{
    [Collection(ClusterCollection.Name)]
    public class BestRoutesAppServiceTests : TradeTestBase
    {
        private readonly IBestRoutesAppService _bestRoutesAppService;
        private readonly ITradePairAppService _tradePairAppService;
        private readonly TradePairTestHelper _tradePairTestHelper;
        protected Guid TradePairEthUsdtFee2Id { get; }
        protected string TradePairEthUsdtFee2Address { get; }
        protected Guid TradePairBtcUsdtFee2Id { get; }
        protected string TradePairBtcUsdtFee2Address { get; }
        
        public BestRoutesAppServiceTests()
        {
            _bestRoutesAppService = GetRequiredService<IBestRoutesAppService>();
            _tradePairAppService = GetRequiredService<ITradePairAppService>();
            _tradePairTestHelper = GetRequiredService<TradePairTestHelper>();
            
            var tradePairEthUsdtFee2 = AsyncHelper.RunSync(async () => await _tradePairTestHelper.CreateAsync(
                new TradePairCreateDto
                {
                    ChainId = ChainId,
                    Address = "0xPool006a6FaC8c710e53c4B2c2F96477119dA364",
                    Id = Guid.Parse("3F2504E0-4F89-41D3-9A0C-0305E82C3304"),
                    Token0Id = TokenEthId,
                    Token1Id = TokenUsdtId,
                    FeeRate = 0.001
                }));
            TradePairEthUsdtFee2Id = tradePairEthUsdtFee2.Id;
            TradePairEthUsdtFee2Address = tradePairEthUsdtFee2.Address;
            
            var tradePairBtcUsdtFee2 = AsyncHelper.RunSync(async () => await _tradePairTestHelper.CreateAsync(
                new TradePairCreateDto
                {
                    ChainId = ChainId,
                    Address = "0xPool006a6FaC8c710e53c4B2c2F96477119dA365",
                    Id = Guid.Parse("3D2504E0-4F89-41D3-9A0C-0305E82C3305"),
                    Token0Id = TokenBtcId,
                    Token1Id = TokenUsdtId,
                    FeeRate = 0.001,
                }));
            TradePairBtcUsdtFee2Id = tradePairBtcUsdtFee2.Id;
            TradePairBtcUsdtFee2Address = tradePairBtcUsdtFee2.Address;
            
            AsyncHelper.RunSync(async () => await _tradePairAppService.CreateSyncAsync(new SyncRecordDto()
            {
                ChainId = ChainId,
                PairAddress = TradePairEthUsdtAddress,
                SymbolA = TokenEthSymbol,
                SymbolB = TokenUsdtSymbol,
                ReserveA = NumberFormatter.WithDecimals(100, 8),
                ReserveB = NumberFormatter.WithDecimals(10000, 6),
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.AddDays(-2))
            }));
            AsyncHelper.RunSync(async () => await _tradePairAppService.CreateSyncAsync(new SyncRecordDto()
            {
                ChainId = ChainId,
                PairAddress = TradePairEthUsdtFee2Address,
                SymbolA = TokenEthSymbol,
                SymbolB = TokenUsdtSymbol,
                ReserveA = NumberFormatter.WithDecimals(200, 8),
                ReserveB = NumberFormatter.WithDecimals(20000, 6),
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.AddDays(-2))
            }));
            
            AsyncHelper.RunSync(async () => await _tradePairAppService.CreateSyncAsync(new SyncRecordDto()
            {
                ChainId = ChainId,
                PairAddress = TradePairBtcUsdtAddress,
                SymbolA = TokenBtcSymbol,
                SymbolB = TokenUsdtSymbol,
                ReserveA = NumberFormatter.WithDecimals(100, 8),
                ReserveB = NumberFormatter.WithDecimals(10000, 6),
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.AddDays(-2))
            }));
            AsyncHelper.RunSync(async () => await _tradePairAppService.CreateSyncAsync(new SyncRecordDto()
            {
                ChainId = ChainId,
                PairAddress = TradePairBtcUsdtFee2Address,
                SymbolA = TokenBtcSymbol,
                SymbolB = TokenUsdtSymbol,
                ReserveA = NumberFormatter.WithDecimals(200, 8),
                ReserveB = NumberFormatter.WithDecimals(20000, 6),
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.AddDays(-2))
            }));
        }
        
        [Fact]
        public async Task GetAmountsInTest()
        {
            var tokens = new List<string>() { TokenEthSymbol, TokenUsdtSymbol, TokenBtcSymbol };
            var tradePairIds = new List<Guid>() { TradePairEthUsdtId, TradePairBtcUsdtId };
            var exactAmountOut = NumberFormatter.WithDecimals(10, 8);
            var amountsIn = await _bestRoutesAppService.GetAmountsInAsync(tokens, tradePairIds, exactAmountOut);
            amountsIn.Count.ShouldBe(3);
            amountsIn[0].ShouldBe(1294308216);
            amountsIn[1].ShouldBe(1145475373);
            amountsIn[2].ShouldBe(exactAmountOut);
        }
        
        [Fact]
        public async Task GetAmountsOutTest()
        {
            var tokens = new List<string>() { TokenEthSymbol, TokenUsdtSymbol, TokenBtcSymbol };
            var tradePairIds = new List<Guid>() { TradePairEthUsdtId, TradePairBtcUsdtId };
            var exactAmountIn = NumberFormatter.WithDecimals(10, 8);
            var amountsOut = await _bestRoutesAppService.GetAmountsOutAsync(tokens, tradePairIds, exactAmountIn);
            amountsOut.Count.ShouldBe(3);
            amountsOut[0].ShouldBe(exactAmountIn);
            amountsOut[1].ShouldBe(908677667);
            amountsOut[2].ShouldBe(810020707);
        }
        
        [Fact]
        public async Task GetBestRouteExactInTest()
        {
            var result = await _bestRoutesAppService.GetBestRoutesAsync(new GetBestRoutesInput()
            {
                ChainId = ChainId,
                AmountIn = NumberFormatter.WithDecimals(300, 8),
                RouteType = RouteType.ExactIn,
                SymbolIn = TokenEthSymbol,
                SymbolOut = TokenBtcSymbol,
                ResultCount = 3
            });
            result.Routes.Count.ShouldBe(3);
            result.Routes[0].AmountOut.ShouldBe("9922171425");
            result.Routes[1].AmountOut.ShouldBe("9915173414");
            result.Routes[2].AmountOut.ShouldBe("9881563199");
            result.Routes[0].Distributions[0].TradePairs.Count.ShouldBe(2);
            result.Routes[0].Distributions[0].TradePairExtensions.Count.ShouldBe(2);
        }
        
        [Fact]
        public async Task GetBestRouteExactOutTest()
        {
            var result = await _bestRoutesAppService.GetBestRoutesAsync(new GetBestRoutesInput()
            {
                ChainId = ChainId,
                AmountOut = NumberFormatter.WithDecimals(50, 8),
                RouteType = RouteType.ExactOut,
                SymbolIn = TokenEthSymbol,
                SymbolOut = TokenBtcSymbol,
                ResultCount = 3
            });
            result.Routes.Count.ShouldBe(3);
            result.Routes[0].AmountIn.ShouldBe("7621837271");
            result.Routes[1].AmountIn.ShouldBe("7625902385");
            result.Routes[2].AmountIn.ShouldBe("7741803687");
        }
    }
}