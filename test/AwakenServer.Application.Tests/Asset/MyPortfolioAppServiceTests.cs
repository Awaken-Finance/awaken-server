using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.MyPortfolio;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Grains.Tests;
using AwakenServer.Price;
using AwakenServer.Provider;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Index;
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
    private readonly INESTRepository<UserLiquiditySnapshotIndex, Guid> _userLiquiditySnapshotIndexRepository;
    private readonly INESTRepository<TradePairMarketDataSnapshot, Guid> _tradePairSnapshotIndexRepository;
    private readonly ITradePairMarketDataProvider _tradePairMarketDataProvider;
    private readonly IMyPortfolioAppService _myPortfolioAppService;
    
    protected readonly string UserAddress = "0x123456789";
    public MyPortfolioAppServiceTests()
    {
        _graphQlProvider = GetRequiredService<MockGraphQLProvider>();
        _assetAppService = GetRequiredService<IAssetAppService>();
        _priceAppService = GetRequiredService<IPriceAppService>();
        _currentUserLiquidityIndexRepository = GetRequiredService<INESTRepository<CurrentUserLiquidityIndex, Guid>>();
        _userLiquiditySnapshotIndexRepository = GetRequiredService<INESTRepository<UserLiquiditySnapshotIndex, Guid>>();
        _tradePairSnapshotIndexRepository = GetRequiredService<INESTRepository<TradePairMarketDataSnapshot, Guid>>();
        _tradePairMarketDataProvider = GetRequiredService<ITradePairMarketDataProvider>();
        _myPortfolioAppService = GetRequiredService<IMyPortfolioAppService>();
    }
    
    private async Task PrepareTradePairData()
    {
        await _tradePairMarketDataProvider.AddOrUpdateSnapshotAsync(TradePairBtcUsdtId, async grain =>
        {
            return await grain.UpdateTotalSupplyAsync(new LiquidityRecordGrainDto()
            {
                ChainId = ChainName,
                Timestamp = DateTime.Now.AddDays(-3),
                Type = LiquidityType.Mint,
                LpTokenAmount = "1",
                TotalSupply = "1",
                BlockHeight = 100
            });
        });
        
        await _tradePairMarketDataProvider.AddOrUpdateSnapshotAsync(TradePairBtcUsdtId, async grain =>
        {
            return await grain.UpdatePriceAsync(new SyncRecordGrainDto()
            {
                ChainId = ChainName,
                PairAddress = TradePairBtcUsdtAddress,
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.Now.AddDays(-3)),
                ReserveA = NumberFormatter.WithDecimals(10, 8),
                ReserveB = NumberFormatter.WithDecimals(90, 6),
                BlockHeight = 101,
                SymbolA = "BTC",
                SymbolB = "USDT",
                Token0PriceInUsd = 1,
                Token1PriceInUsd = 1
            });
        });
    }

    private async Task SyncAddLiquidityRecordTest()
    {
        var inputMint = new LiquidityRecordDto()
        {
            ChainId = ChainName,
            Pair = TradePairBtcUsdtAddress,
            Address = "0x123456789",
            Timestamp = 1000,
            Token0Amount = 100,
            Token0 = "BTC",
            Token1Amount = 1000,
            Token1 = "USDT",
            LpTokenAmount = 50000,
            Type = LiquidityType.Mint,
            TransactionHash = "0xdab24d0f0c28a3be6b59332ab0cb0b4cd54f10f3c1b12cfc81d72e934d74b28f",
            Channel = "TestChanel",
            Sender = "0x123456789",
            To = "0x123456789",
            BlockHeight = 100
        };
        var syncResult = await _myPortfolioAppService.SyncLiquidityRecordAsync(inputMint);
        syncResult.ShouldBeTrue();

        var currentTradePairGrain =
            Cluster.Client.GetGrain<ICurrentTradePairGrain>(GrainIdHelper.GenerateGrainId(TradePairBtcUsdtId));
        var currentTradePairResult = await currentTradePairGrain.GetAsync();
        currentTradePairResult.Success.ShouldBeTrue();
        currentTradePairResult.Data.LastUpdateTime.ShouldBe(DateTimeHelper.FromUnixTimeMilliseconds(1000));
        currentTradePairResult.Data.TotalSupply.ShouldBe(50000);


        var currentUserLiquidityIndex = await _currentUserLiquidityIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.TradePairId).Value(TradePairBtcUsdtId)) &&
            q.Term(i => i.Field(f => f.Address).Value(inputMint.Address)));
        currentUserLiquidityIndex.LpTokenAmount.ShouldBe(50000);
        currentUserLiquidityIndex.Token0CumulativeAddition.ShouldBe(100);
        currentUserLiquidityIndex.Token1CumulativeAddition.ShouldBe(1000);
        currentUserLiquidityIndex.LastUpdateTime.ShouldBe(DateTimeHelper.FromUnixTimeMilliseconds(1000));
        currentUserLiquidityIndex.AverageHoldingStartTime.ShouldBe(currentUserLiquidityIndex.LastUpdateTime);

        var snapshotTime = currentUserLiquidityIndex.LastUpdateTime.Date;
        var snapshotIndex = await _userLiquiditySnapshotIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.TradePairId).Value(TradePairBtcUsdtId)) &&
            q.Term(i => i.Field(f => f.Address).Value(inputMint.Address)) &&
            q.Term(i => i.Field(f => f.SnapShotTime).Value(snapshotTime)));
        snapshotIndex.LpTokenAmount.ShouldBe(50000);
    }


    [Fact]
    public async Task SyncSecondAddAndRemoveLiquidityRecordTest()
    {
        await SyncAddLiquidityRecordTest();
        
        var inputMint1 = new LiquidityRecordDto()
        {
            ChainId = ChainName,
            Pair = TradePairBtcUsdtAddress,
            Address = "0x123456789",
            Timestamp = 2000,
            Token0Amount = 100,
            Token0 = "BTC",
            Token1Amount = 1000,
            Token1 = "USDT",
            LpTokenAmount = 50000,
            Type = LiquidityType.Mint,
            TransactionHash = "0xdab24d0f0c28a3be6b59332ab0cb0b4cd54f10f3c1b12cfc81d72e934d74b28f1",
            Channel = "TestChanel",
            Sender = "0x123456789",
            To = "0x123456789",
            BlockHeight = 200
        };
        await _myPortfolioAppService.SyncLiquidityRecordAsync(inputMint1);
        
        var currentTradePairGrain =
            Cluster.Client.GetGrain<ICurrentTradePairGrain>(GrainIdHelper.GenerateGrainId(TradePairBtcUsdtId));
        var currentTradePairResult = await currentTradePairGrain.GetAsync();
        currentTradePairResult.Success.ShouldBeTrue();
        currentTradePairResult.Data.LastUpdateTime.ShouldBe(DateTimeHelper.FromUnixTimeMilliseconds(2000));
        currentTradePairResult.Data.TotalSupply.ShouldBe(100000);

        var currentUserLiquidityIndex = await _currentUserLiquidityIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.TradePairId).Value(TradePairBtcUsdtId)) &&
            q.Term(i => i.Field(f => f.Address).Value(inputMint1.Address)));
        currentUserLiquidityIndex.LpTokenAmount.ShouldBe(100000);
        currentUserLiquidityIndex.Token0CumulativeAddition.ShouldBe(200);
        currentUserLiquidityIndex.Token1CumulativeAddition.ShouldBe(2000);
        currentUserLiquidityIndex.LastUpdateTime.ShouldBe(DateTimeHelper.FromUnixTimeMilliseconds(2000));
        currentUserLiquidityIndex.AverageHoldingStartTime.ShouldBe(DateTimeHelper.FromUnixTimeMilliseconds(1500));
        
        var snapshotTime = currentUserLiquidityIndex.LastUpdateTime.Date;
        var snapshotIndex = await _userLiquiditySnapshotIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.TradePairId).Value(TradePairBtcUsdtId)) &&
            q.Term(i => i.Field(f => f.Address).Value(inputMint1.Address)) &&
            q.Term(i => i.Field(f => f.SnapShotTime).Value(snapshotTime)));
        snapshotIndex.LpTokenAmount.ShouldBe(100000);
        
        var inputBurn = new LiquidityRecordDto()
        {
            ChainId = ChainName,
            Pair = TradePairBtcUsdtAddress,
            Address = "0x123456789",
            Timestamp = 3000,
            Token0Amount = 100,
            Token0 = "BTC",
            Token1Amount = 1000,
            Token1 = "USDT",
            LpTokenAmount = 50000,
            Type = LiquidityType.Burn,
            TransactionHash = "0xdab24d0f0c28a3be6b59332ab0cb0b4cd54f10f3c1b12cfc81d72e934d74b28f2",
            Channel = "TestChanel",
            Sender = "0x123456789",
            To = "0x123456789",
            BlockHeight = 300
        };
        await _myPortfolioAppService.SyncLiquidityRecordAsync(inputBurn);
        currentTradePairResult = await currentTradePairGrain.GetAsync();
        currentTradePairResult.Success.ShouldBeTrue();
        currentTradePairResult.Data.LastUpdateTime.ShouldBe(DateTimeHelper.FromUnixTimeMilliseconds(3000));
        currentTradePairResult.Data.TotalSupply.ShouldBe(50000);
        
        
        currentUserLiquidityIndex = await _currentUserLiquidityIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.TradePairId).Value(TradePairBtcUsdtId)) &&
            q.Term(i => i.Field(f => f.Address).Value(inputMint1.Address)));
        currentUserLiquidityIndex.LpTokenAmount.ShouldBe(50000);
        currentUserLiquidityIndex.Token0CumulativeAddition.ShouldBe(100);
        currentUserLiquidityIndex.Token1CumulativeAddition.ShouldBe(1000);
        currentUserLiquidityIndex.LastUpdateTime.ShouldBe(DateTimeHelper.FromUnixTimeMilliseconds(3000));
        currentUserLiquidityIndex.AverageHoldingStartTime.ShouldBe(DateTimeHelper.FromUnixTimeMilliseconds(1500));
        
        snapshotTime = currentUserLiquidityIndex.LastUpdateTime.Date;
        snapshotIndex = await _userLiquiditySnapshotIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.TradePairId).Value(TradePairBtcUsdtId)) &&
            q.Term(i => i.Field(f => f.Address).Value(inputMint1.Address)) &&
            q.Term(i => i.Field(f => f.SnapShotTime).Value(snapshotTime)));
        snapshotIndex.LpTokenAmount.ShouldBe(50000);

    }
    
    private async Task SyncSwapRecordTest()
    {
        await SyncAddLiquidityRecordTest();
        var swapRecordDto = new SwapRecordDto
        {
            ChainId = "tDVV",
            PairAddress = TradePairBtcUsdtAddress,
            Sender = "TV2aRV4W5oSJzxrkBvj8XmJKkMCiEQnAvLmtM9BqLTN3beXm2",
            TransactionHash = "6622966a928185655d691565d6128835e7d1ccdf1dd3b5f277c5f2a5b2802d37",
            Timestamp = 4000,
            AmountOut = NumberFormatter.WithDecimals(1000, 8),
            AmountIn = NumberFormatter.WithDecimals(1000, 6),
            SymbolOut = TokenBtcSymbol,
            SymbolIn = TokenUsdtSymbol,
            TotalFee = 100,
            Channel = "test",
            BlockHeight = 99,
        };
        await _myPortfolioAppService.SyncSwapRecordAsync(swapRecordDto);
        var swapRecordDto1 = new SwapRecordDto
        {
            ChainId = "tDVV",
            PairAddress = TradePairBtcUsdtAddress,
            Sender = "TV2aRV4W5oSJzxrkBvj8XmJKkMCiEQnAvLmtM9BqLTN3beXm2",
            TransactionHash = "6622966a928185655d691565d6128835e7d1ccdf1dd3b5f277c5f2a5b2802d38",
            Timestamp = 4000,
            AmountOut = NumberFormatter.WithDecimals(1000, 8),
            AmountIn = NumberFormatter.WithDecimals(1000, 6),
            SymbolOut = TokenUsdtSymbol,
            SymbolIn = TokenBtcSymbol,
            TotalFee = 10,
            Channel = "test",
            BlockHeight = 99,
        };
        await _myPortfolioAppService.SyncSwapRecordAsync(swapRecordDto1);
        
        var currentTradePairGrain =
            Cluster.Client.GetGrain<ICurrentTradePairGrain>(GrainIdHelper.GenerateGrainId(TradePairBtcUsdtId));
        var currentTradePairResult = await currentTradePairGrain.GetAsync();
        currentTradePairResult.Success.ShouldBeTrue();
        currentTradePairResult.Data.TotalSupply.ShouldBe(50000);
        currentTradePairResult.Data.Token0TotalFee.ShouldBe(10);
        currentTradePairResult.Data.Token1TotalFee.ShouldBe(100);
        
        var currentUserLiquidityIndex = await _currentUserLiquidityIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.TradePairId).Value(TradePairBtcUsdtId)) &&
            q.Term(i => i.Field(f => f.Address).Value("0x123456789")));
        currentUserLiquidityIndex.Token0UnReceivedFee.ShouldBe(10);
        currentUserLiquidityIndex.Token1UnReceivedFee.ShouldBe(100);
        
        var snapshotTime = currentUserLiquidityIndex.LastUpdateTime.Date;
        var snapshotIndex = await _userLiquiditySnapshotIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.TradePairId).Value(TradePairBtcUsdtId)) &&
            q.Term(i => i.Field(f => f.Address).Value("0x123456789")) &&
            q.Term(i => i.Field(f => f.SnapShotTime).Value(snapshotTime)));
        snapshotIndex.Token0TotalFee.ShouldBe(10);
        snapshotIndex.Token1TotalFee.ShouldBe(100);
    }

    [Fact]
    public async Task SyncRemoveLiquidityAfterSwapRecordTests()
    {
        await SyncSwapRecordTest();
        var inputBurn = new LiquidityRecordDto()
        {
            ChainId = ChainName,
            Pair = TradePairBtcUsdtAddress,
            Address = "0x123456789",
            Timestamp = 5000,
            Token0Amount = 50,
            Token0 = "BTC",
            Token1Amount = 500,
            Token1 = "USDT",
            LpTokenAmount = 25000,
            Type = LiquidityType.Burn,
            TransactionHash = "0xdab24d0f0c28a3be6b59332ab0cb0b4cd54f10f3c1b12cfc81d72e934d74b28f3",
            Channel = "TestChanel",
            Sender = "0x123456789",
            To = "0x123456789",
            BlockHeight = 400
        };
        await _myPortfolioAppService.SyncLiquidityRecordAsync(inputBurn);
        
        var currentTradePairGrain =
            Cluster.Client.GetGrain<ICurrentTradePairGrain>(GrainIdHelper.GenerateGrainId(TradePairBtcUsdtId));
        var currentTradePairResult = await currentTradePairGrain.GetAsync();
        currentTradePairResult.Success.ShouldBeTrue();
        currentTradePairResult.Data.LastUpdateTime.ShouldBe(DateTimeHelper.FromUnixTimeMilliseconds(5000));
        currentTradePairResult.Data.TotalSupply.ShouldBe(25000);
        
        
        var currentUserLiquidityIndex = await _currentUserLiquidityIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.TradePairId).Value(TradePairBtcUsdtId)) &&
            q.Term(i => i.Field(f => f.Address).Value(inputBurn.Address)));
        currentUserLiquidityIndex.LpTokenAmount.ShouldBe(25000);
        currentUserLiquidityIndex.Token0CumulativeAddition.ShouldBe(55);
        currentUserLiquidityIndex.Token1CumulativeAddition.ShouldBe(550);
        currentUserLiquidityIndex.Token0UnReceivedFee.ShouldBe(5);
        currentUserLiquidityIndex.Token1UnReceivedFee.ShouldBe(50);
        currentUserLiquidityIndex.Token0ReceivedFee.ShouldBe(5);
        currentUserLiquidityIndex.Token1ReceivedFee.ShouldBe(50);
        currentUserLiquidityIndex.LastUpdateTime.ShouldBe(DateTimeHelper.FromUnixTimeMilliseconds(5000));
        currentUserLiquidityIndex.AverageHoldingStartTime.ShouldBe(DateTimeHelper.FromUnixTimeMilliseconds(1000));
        
        var snapshotTime = currentUserLiquidityIndex.LastUpdateTime.Date;
        var snapshotIndex = await _userLiquiditySnapshotIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.TradePairId).Value(TradePairBtcUsdtId)) &&
            q.Term(i => i.Field(f => f.Address).Value(inputBurn.Address)) &&
            q.Term(i => i.Field(f => f.SnapShotTime).Value(snapshotTime)));
        snapshotIndex.LpTokenAmount.ShouldBe(25000);
    }
    
    [Fact]
    public async Task GetUserPositionTest()
    {
        await PrepareTradePairData();
        await SyncSwapRecordTest();

        var result = await _myPortfolioAppService.GetUserPositionsAsync(new GetUserPositionsDto()
        {
            ChainId = ChainName,
            Address = UserAddress,
        });
        result.Items.Count.ShouldBe(1);
        result.Items[0].LpTokenAmount.ShouldBe("0.0005");
        result.Items[0].Token0Amount.ShouldBe("0.005");
        result.Items[0].Token1Amount.ShouldBe("0.045");
        result.Items[0].Position.ValueInUsd.ShouldBe("0.05");
        result.Items[0].Position.Token0ValueInUsd.Substring(0,5).ShouldBe("0.005");
        result.Items[0].Position.Token1ValueInUsd.Substring(0,5).ShouldBe("0.045");
        result.Items[0].CumulativeAddition.ValueInUsd.Substring(0,5).ShouldBe("0.001");
        result.Items[0].CumulativeAddition.Token0ValueInUsd.ShouldBe("1E-06");
        result.Items[0].CumulativeAddition.Token1ValueInUsd.ShouldBe("0.001");
        result.Items[0].Fee.ValueInUsd.Substring(0,6).ShouldBe("0.0001");
        result.Items[0].Fee.Token0ValueInUsd.ShouldBe("1E-07");
        result.Items[0].Fee.Token1ValueInUsd.ShouldBe("0.0001");
        result.Items[0].DynamicAPR.Substring(0, 5).ShouldBe("0.885");
        result.Items[0].ImpermanentLossInUSD.ShouldBe("0.048999");
        result.Items[0].EstimatedAPR[2].Type.ShouldBe(EstimatedAprType.All);
        result.Items[0].EstimatedAPR[2].Percent.Substring(0, 5).ShouldBe("0.180");
    }
    
    [Fact]
    public async Task GetEstimatedAPRWeekTest()
    {
        await PrepareTradePairData();
        await SyncAddLiquidityRecordTest();
        var swapRecordDto = new SwapRecordDto
        {
            ChainId = "tDVV",
            PairAddress = TradePairBtcUsdtAddress,
            Sender = "TV2aRV4W5oSJzxrkBvj8XmJKkMCiEQnAvLmtM9BqLTN3beXm2",
            TransactionHash = "6622966a928185655d691565d6128835e7d1ccdf1dd3b5f277c5f2a5b2802d37",
            Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.AddDays(-1)),
            AmountOut = NumberFormatter.WithDecimals(1000, 8),
            AmountIn = NumberFormatter.WithDecimals(1000, 6),
            SymbolOut = TokenBtcSymbol,
            SymbolIn = TokenUsdtSymbol,
            TotalFee = 5,
            Channel = "test",
            BlockHeight = 99,
        };
        await _myPortfolioAppService.SyncSwapRecordAsync(swapRecordDto);

        var result = await _myPortfolioAppService.GetUserPositionsAsync(new GetUserPositionsDto()
        {
            ChainId = ChainName,
            Address = UserAddress,
        });
        result.Items.Count.ShouldBe(1);
        result.Items[0].EstimatedAPR[0].Type.ShouldBe(EstimatedAprType.Week);
        result.Items[0].EstimatedAPR[0].Percent.Substring(0, 3).ShouldBe("1.8");
        result.Items[0].EstimatedAPR[1].Type.ShouldBe(EstimatedAprType.Month);
        result.Items[0].EstimatedAPR[1].Percent.Substring(0, 3).ShouldBe("1.8");
    }

    [Fact]
    public async Task GetUserPortfolioTest()
    {
        await PrepareTradePairData();
        await SyncSwapRecordTest();
        var result = await _myPortfolioAppService.GetUserPortfolioAsync(new GetUserPortfolioDto()
        {
            ChainId = ChainName,
            Address = UserAddress,
        });
        result.TradePairPositionDistributions.Count.ShouldBe(1);
        result.TradePairPositionDistributions[0].ValueInUsd.ShouldBe("0.05");
        result.TradePairPositionDistributions[0].ValuePercent.ShouldBe("1");
        result.TradePairFeeDistributions.Count.ShouldBe(1);
        result.TradePairFeeDistributions[0].ValueInUsd.Substring(0,6).ShouldBe("0.0001");
        result.TradePairFeeDistributions[0].ValuePercent.ShouldBe("1");
        result.TokenPositionDistributions.Count.ShouldBe(2);
        result.TokenPositionDistributions[0].ValueInUsd.Substring(0,5).ShouldBe("0.045");
        result.TokenPositionDistributions[0].ValuePercent.Substring(0,3).ShouldBe("0.9");
        result.TokenFeeDistributions[0].ValuePercent.ShouldBe("0.9");
    }
}