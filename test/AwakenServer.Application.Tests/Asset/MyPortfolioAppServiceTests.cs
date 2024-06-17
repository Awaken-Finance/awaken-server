using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Grains.Tests;
using AwakenServer.Price;
using AwakenServer.Provider;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Index;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using MongoDB.Driver.Linq;
using Shouldly;
using Xunit;
using TradePairMarketDataSnapshot = AwakenServer.Trade.TradePairMarketDataSnapshot;

namespace AwakenServer.Asset;

[Collection(ClusterCollection.Name)]
public class MyPortfolioAppServiceTests : TradeTestBase
{
    private readonly MockGraphQLProvider _graphQlProvider;
    private readonly IAssetAppService _assetAppService;
    private readonly IPriceAppService _priceAppService;
    private readonly INESTRepository<CurrentUserLiquidityIndex, Guid> _currentUserLiquidityIndexRepository;
    private readonly INESTRepository<UserLiquiditySnapshotIndex, Guid> _userLiduiditySnapshotIndexRepository;
    private readonly INESTRepository<TradePairMarketDataSnapshot, Guid> _tradePairSnapshotIndexRepository;
    private readonly ITradePairMarketDataProvider _tradePairMarketDataProvider;
    private readonly IMyPortfolioAppService _myPortfolioAppService;
    
    protected readonly string UserAddress = "0x1";
    
    public MyPortfolioAppServiceTests()
    {
        _graphQlProvider = GetRequiredService<MockGraphQLProvider>();
        _assetAppService = GetRequiredService<IAssetAppService>();
        _priceAppService = GetRequiredService<IPriceAppService>();
        _currentUserLiquidityIndexRepository = GetRequiredService<INESTRepository<CurrentUserLiquidityIndex, Guid>>();
        _userLiduiditySnapshotIndexRepository = GetRequiredService<INESTRepository<UserLiquiditySnapshotIndex, Guid>>();
        _tradePairSnapshotIndexRepository = GetRequiredService<INESTRepository<TradePairMarketDataSnapshot, Guid>>();
        _tradePairMarketDataProvider = GetRequiredService<ITradePairMarketDataProvider>();
        _myPortfolioAppService = GetRequiredService<IMyPortfolioAppService>();
    }

    private async Task PrepareTradePairData()
    {
        await _tradePairMarketDataProvider.AddOrUpdateSnapshotAsync(TradePairEthUsdtId, async grain =>
        {
            return await grain.UpdateTotalSupplyAsync(new LiquidityRecordGrainDto()
            {
                ChainId = ChainName,
                Timestamp = DateTime.Now.AddDays(-3),
                Type = LiquidityType.Mint,
                LpTokenAmount = "1000000",
                TotalSupply = "1000000",
                BlockHeight = 100
            });
        });
        
        await _tradePairMarketDataProvider.AddOrUpdateSnapshotAsync(TradePairEthUsdtId, async grain =>
        {
            return await grain.UpdatePriceAsync(new SyncRecordGrainDto()
            {
                ChainId = ChainName,
                PairAddress = TradePairEthUsdtAddress,
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.Now.AddDays(-2)),
                ReserveA = 100000000,
                ReserveB = 1000000,
                BlockHeight = 101,
                SymbolA = "ETH",
                SymbolB = "USDT",
                Token0PriceInUsd = 1,
                Token1PriceInUsd = 1
            });
        });
    }

    private async Task PrepareUserData()
    {
        
        var index1 = new CurrentUserLiquidityIndex
        {
            TradePairId = TradePairEthUsdtId,
            Address = UserAddress,
            ChainId = ChainId,
            LpTokenAmount = 10000
        };
        await _currentUserLiquidityIndexRepository.AddAsync(index1);

        var index2 = new UserLiquiditySnapshotIndex
        {
            TradePairId = TradePairEthUsdtId,
            Address = UserAddress,
            LpTokenAmount = 10000,
            SnapShotTime = DateTime.Today,
            Token0TotalFee = 10,
            Token1TotalFee = 10
        };
        await _userLiduiditySnapshotIndexRepository.AddAsync(index2);
    }
    
    [Fact]
    public async Task GetUserPositionTest()
    {
        await PrepareTradePairData();
        await PrepareUserData();

        var result = await _myPortfolioAppService.GetUserPositionsAsync(new GetUserPositionsDto()
        {
            ChainId = ChainName,
            Address = UserAddress,
            EstimatedAprType = (int)EstimatedAprType.All
        });
        result.Items.Count.ShouldBe(1);
        
        result = await _myPortfolioAppService.GetUserPositionsAsync(new GetUserPositionsDto()
        {
            ChainId = ChainName,
            Address = UserAddress,
            EstimatedAprType = (int)EstimatedAprType.Week
        });
        result.Items.Count.ShouldBe(1);
        
        result = await _myPortfolioAppService.GetUserPositionsAsync(new GetUserPositionsDto()
        {
            ChainId = ChainName,
            Address = UserAddress,
            EstimatedAprType = (int)EstimatedAprType.Month
        });
        result.Items.Count.ShouldBe(1);
    }
    
    [Fact]
    public async Task GetUserPortfolioTest()
    {
        await PrepareTradePairData();
        await PrepareUserData();

        var result = await _myPortfolioAppService.GetUserPortfolioAsync(new GetUserPortfolioDto()
        {
            ChainId = ChainName,
            Address = UserAddress,
        });
        result.TradePairDistributions.Count.ShouldBe(1);
        result.TokenDistributions.Count.ShouldBe(2);
    }
}