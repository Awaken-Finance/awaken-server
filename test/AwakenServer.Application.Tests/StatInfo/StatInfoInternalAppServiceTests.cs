using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Grains.Grain.StatInfo;
using AwakenServer.Grains.Tests;
using AwakenServer.StatInfo.Index;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;
using TradePair = AwakenServer.Trade.Index.TradePair;

namespace AwakenServer.StatInfo;

[Collection(ClusterCollection.Name)]
public class StatInfoInternalAppServiceTests : TradeTestBase
{
    private readonly IStatInfoInternalAppService _statInfoInternalAppService;
    private readonly INESTRepository<TradePair, Guid> _tradePairIndexRepository;
    private readonly INESTRepository<TokenStatInfoIndex, Guid> _tokenStatInfoIndexRepository;
    private readonly INESTRepository<PoolStatInfoIndex, Guid> _poolStatInfoIndexRepository;
    private readonly INESTRepository<TransactionHistoryIndex, Guid> _transactionIndexRepository;
    private readonly INESTRepository<StatInfoSnapshotIndex, Guid> _statInfoSnapshotIndexRepository;
    private readonly IOptionsSnapshot<StatInfoOptions> _statInfoOptions;

    public StatInfoInternalAppServiceTests()
    {
        _statInfoInternalAppService = GetRequiredService<IStatInfoInternalAppService>();
        _tradePairIndexRepository = GetRequiredService<INESTRepository<TradePair, Guid>>();
        _tokenStatInfoIndexRepository = GetRequiredService<INESTRepository<TokenStatInfoIndex, Guid>>();
        _poolStatInfoIndexRepository = GetRequiredService<INESTRepository<PoolStatInfoIndex, Guid>>();
        _transactionIndexRepository = GetRequiredService<INESTRepository<TransactionHistoryIndex, Guid>>();
        _statInfoSnapshotIndexRepository = GetRequiredService<INESTRepository<StatInfoSnapshotIndex, Guid>>();
        _statInfoOptions = GetRequiredService<IOptionsSnapshot<StatInfoOptions>>();
    }
    private string AddVersionToKey(string baseKey, string version)
    {
        return $"{baseKey}:{version}";
    }
    
    [Fact]
    public async Task SyncAddLiquidityRecordTest()
    {
        var curTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var inputMint = new LiquidityRecordDto()
        {
            ChainId = ChainName,
            Pair = TradePairBtcUsdtAddress,
            Address = "0x123456789",
            Timestamp = curTime,
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
        var syncResult = await _statInfoInternalAppService.CreateLiquidityRecordAsync(inputMint, _statInfoOptions.Value.DataVersion);
        syncResult.ShouldBeTrue();

        // pool
        var poolStatInfoGrain =
            Cluster.Client.GetGrain<IPoolStatInfoGrain>(AddVersionToKey(TradePairBtcUsdtAddress, _statInfoOptions.Value.DataVersion));
        var poolStatInfoResult = await poolStatInfoGrain.GetAsync();
        poolStatInfoResult.Success.ShouldBeTrue();
        poolStatInfoResult.Data.PairAddress.ShouldBe(TradePairBtcUsdtAddress);
        poolStatInfoResult.Data.TransactionCount.ShouldBe(1);
        poolStatInfoResult.Data.LastUpdateTime.ShouldBe(curTime);
        
        var poolStatInfoIndex = await _poolStatInfoIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.PairAddress).Value(TradePairBtcUsdtAddress)) &&
            q.Term(i => i.Field(f => f.Version).Value(_statInfoOptions.Value.DataVersion)));
        poolStatInfoIndex.TransactionCount.ShouldBe(1);
        poolStatInfoIndex.PairAddress.ShouldBe(TradePairBtcUsdtAddress);
        poolStatInfoIndex.TradePair.Id.ShouldBe(TradePairBtcUsdtId);
        poolStatInfoIndex.LastUpdateTime.ShouldBe(curTime);

       // token0
       var token0StatInfoGrain =
           Cluster.Client.GetGrain<ITokenStatInfoGrain>(AddVersionToKey(inputMint.Token0, _statInfoOptions.Value.DataVersion));
       var token0StatInfoResult = await token0StatInfoGrain.GetAsync();
       token0StatInfoResult.Success.ShouldBeTrue();
       token0StatInfoResult.Data.Symbol.ShouldBe(inputMint.Token0);
       token0StatInfoResult.Data.TransactionCount.ShouldBe(1);
       token0StatInfoResult.Data.LastUpdateTime.ShouldBe(curTime);
        
       var token0StatInfoIndex = await _tokenStatInfoIndexRepository.GetAsync(q =>
           q.Term(i => i.Field(f => f.Symbol).Value(inputMint.Token0)) &&
           q.Term(i => i.Field(f => f.Version).Value(_statInfoOptions.Value.DataVersion)));
       token0StatInfoIndex.TransactionCount.ShouldBe(1);
       token0StatInfoIndex.Symbol.ShouldBe(inputMint.Token0);
       token0StatInfoIndex.TransactionCount.ShouldBe(1);
       token0StatInfoIndex.LastUpdateTime.ShouldBe(curTime);
       
       // token1
       var token1StatInfoGrain =
           Cluster.Client.GetGrain<ITokenStatInfoGrain>(AddVersionToKey(inputMint.Token1, _statInfoOptions.Value.DataVersion));
       var token1StatInfoResult = await token1StatInfoGrain.GetAsync();
       token1StatInfoResult.Success.ShouldBeTrue();
       token1StatInfoResult.Data.Symbol.ShouldBe(inputMint.Token1);
       token1StatInfoResult.Data.TransactionCount.ShouldBe(1);
       token1StatInfoResult.Data.LastUpdateTime.ShouldBe(curTime);
        
       var token1StatInfoIndex = await _tokenStatInfoIndexRepository.GetAsync(q =>
           q.Term(i => i.Field(f => f.Symbol).Value(inputMint.Token1)) &&
           q.Term(i => i.Field(f => f.Version).Value(_statInfoOptions.Value.DataVersion)));
       token1StatInfoIndex.TransactionCount.ShouldBe(1);
       token1StatInfoIndex.Symbol.ShouldBe(inputMint.Token1);
       token1StatInfoIndex.TransactionCount.ShouldBe(1);
       token1StatInfoIndex.LastUpdateTime.ShouldBe(curTime);
       
       // transaction history
       var transactionHistoryIndex = await _transactionIndexRepository.GetAsync(q =>
           q.Term(i => i.Field(f => f.TransactionHash).Value(inputMint.TransactionHash)) &&
           q.Term(i => i.Field(f => f.ChainId).Value(inputMint.ChainId)) &&
           q.Term(i => i.Field(f => f.Version).Value(_statInfoOptions.Value.DataVersion)));
       transactionHistoryIndex.TradePair.Id.ShouldBe(TradePairBtcUsdtId);
       transactionHistoryIndex.TransactionType.ShouldBe(TransactionType.Add);
       transactionHistoryIndex.Token0Amount.ShouldBe(inputMint.Token0Amount.ToDecimalsString(8));
       transactionHistoryIndex.Token1Amount.ShouldBe(inputMint.Token1Amount.ToDecimalsString(6));
       transactionHistoryIndex.Timestamp.ShouldBe(curTime);
       transactionHistoryIndex.ValueInUsd.ShouldBe(double.Parse(transactionHistoryIndex.Token0Amount) + double.Parse(transactionHistoryIndex.Token1Amount));
    }

    [Fact]
    public async Task CreateSwapRecordTests()
    {
        long curTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var swapRecordDto = new SwapRecordDto
        {
            ChainId = "tDVV",
            PairAddress = TradePairBtcUsdtAddress,
            Sender = "TV2aRV4W5oSJzxrkBvj8XmJKkMCiEQnAvLmtM9BqLTN3beXm2",
            TransactionHash = "6622966a928185655d691565d6128835e7d1ccdf1dd3b5f277c5f2a5b2802d37",
            Timestamp = curTime,
            AmountOut = NumberFormatter.WithDecimals(1000, 8),
            AmountIn = NumberFormatter.WithDecimals(100, 6),
            SymbolOut = TokenBtcSymbol,
            SymbolIn = TokenUsdtSymbol,
            TotalFee = 100,
            Channel = "test",
            BlockHeight = 99,
        };
        var result = await _statInfoInternalAppService.CreateSwapRecordAsync(swapRecordDto, _statInfoOptions.Value.DataVersion);
        result.ShouldBeTrue();
        // pool
        var poolStatInfoGrain =
            Cluster.Client.GetGrain<IPoolStatInfoGrain>(AddVersionToKey(TradePairBtcUsdtAddress, _statInfoOptions.Value.DataVersion));
        var poolStatInfoResult = await poolStatInfoGrain.GetAsync();
        poolStatInfoResult.Success.ShouldBeTrue();
        poolStatInfoResult.Data.PairAddress.ShouldBe(TradePairBtcUsdtAddress);
        poolStatInfoResult.Data.TransactionCount.ShouldBe(1);
        poolStatInfoResult.Data.LastUpdateTime.ShouldBe(curTime);
        poolStatInfoResult.Data.VolumeInUsd7d.ShouldBe(1000);
        poolStatInfoResult.Data.VolumeInUsd24h.ShouldBe(1000);
        
        var poolStatInfoIndex = await _poolStatInfoIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.PairAddress).Value(TradePairBtcUsdtAddress)) &&
            q.Term(i => i.Field(f => f.Version).Value(_statInfoOptions.Value.DataVersion)));
        poolStatInfoIndex.TransactionCount.ShouldBe(1);
        poolStatInfoIndex.PairAddress.ShouldBe(TradePairBtcUsdtAddress);
        poolStatInfoIndex.TradePair.Id.ShouldBe(TradePairBtcUsdtId);
        poolStatInfoIndex.LastUpdateTime.ShouldBe(curTime);
        poolStatInfoIndex.VolumeInUsd7d.ShouldBe(1000);
        poolStatInfoIndex.VolumeInUsd24h.ShouldBe(1000);

       // token0
       var token0StatInfoGrain =
           Cluster.Client.GetGrain<ITokenStatInfoGrain>(AddVersionToKey("BTC", _statInfoOptions.Value.DataVersion));
       var token0StatInfoResult = await token0StatInfoGrain.GetAsync();
       token0StatInfoResult.Success.ShouldBeTrue();
       token0StatInfoResult.Data.Symbol.ShouldBe("BTC");
       token0StatInfoResult.Data.TransactionCount.ShouldBe(1);
       token0StatInfoResult.Data.LastUpdateTime.ShouldBe(curTime);
       token0StatInfoResult.Data.VolumeInUsd24h.ShouldBe(1000);
        
       var token0StatInfoIndex = await _tokenStatInfoIndexRepository.GetAsync(q =>
           q.Term(i => i.Field(f => f.Symbol).Value("BTC")) &&
           q.Term(i => i.Field(f => f.Version).Value(_statInfoOptions.Value.DataVersion)));
       token0StatInfoIndex.TransactionCount.ShouldBe(1);
       token0StatInfoIndex.Symbol.ShouldBe("BTC");
       token0StatInfoIndex.LastUpdateTime.ShouldBe(curTime);
       token0StatInfoIndex.VolumeInUsd24h.ShouldBe(1000);
       
       // token1
       var token1StatInfoGrain =
           Cluster.Client.GetGrain<ITokenStatInfoGrain>(AddVersionToKey("USDT", _statInfoOptions.Value.DataVersion));
       var token1StatInfoResult = await token1StatInfoGrain.GetAsync();
       token1StatInfoResult.Success.ShouldBeTrue();
       token1StatInfoResult.Data.Symbol.ShouldBe("USDT");
       token1StatInfoResult.Data.TransactionCount.ShouldBe(1);
       token1StatInfoResult.Data.LastUpdateTime.ShouldBe(curTime);
       token1StatInfoResult.Data.VolumeInUsd24h.ShouldBe(100);
       
       var token1StatInfoIndex = await _tokenStatInfoIndexRepository.GetAsync(q =>
           q.Term(i => i.Field(f => f.Symbol).Value("USDT")) &&
           q.Term(i => i.Field(f => f.Version).Value(_statInfoOptions.Value.DataVersion)));
       token1StatInfoIndex.TransactionCount.ShouldBe(1);
       token1StatInfoIndex.Symbol.ShouldBe("USDT");
       token1StatInfoIndex.LastUpdateTime.ShouldBe(curTime);
       token1StatInfoIndex.VolumeInUsd24h.ShouldBe(100);
       
       // transaction history
       var transactionHistoryIndex = await _transactionIndexRepository.GetAsync(q =>
           q.Term(i => i.Field(f => f.TransactionHash).Value(swapRecordDto.TransactionHash)) &&
           q.Term(i => i.Field(f => f.ChainId).Value(swapRecordDto.ChainId)) &&
           q.Term(i => i.Field(f => f.Version).Value(_statInfoOptions.Value.DataVersion)));
       transactionHistoryIndex.TradePair.Id.ShouldBe(TradePairBtcUsdtId);
       transactionHistoryIndex.TransactionType.ShouldBe(TransactionType.Trade);
       transactionHistoryIndex.Side.ShouldBe(TradeSide.Buy);
       transactionHistoryIndex.Token0Amount.ShouldBe("1000");
       transactionHistoryIndex.Token1Amount.ShouldBe("100");
       transactionHistoryIndex.Timestamp.ShouldBe(curTime);
       transactionHistoryIndex.ValueInUsd.ShouldBe(1000);
    }

    [Fact]
    public async Task CreateSyncRecordTests()
    {
        await _statInfoInternalAppService.UpdateTokenFollowPairAsync("tDVV", _statInfoOptions.Value.DataVersion);
        var syncRecordDto = new SyncRecordDto()
        {
            ChainId = "tDVV",
            PairAddress = TradePairBtcUsdtAddress,
            TransactionHash = "6622966a928185655d691565d6128835e7d1ccdf1dd3b5f277c5f2a5b2802d37",
            Timestamp = 4000,
            ReserveA = NumberFormatter.WithDecimals(1000, 8),
            ReserveB = NumberFormatter.WithDecimals(100, 6),
            SymbolA = TokenBtcSymbol,
            SymbolB = TokenUsdtSymbol,
            BlockHeight = 99,
        };
        var result = await _statInfoInternalAppService.CreateSyncRecordAsync(syncRecordDto, _statInfoOptions.Value.DataVersion);
        result.ShouldBeTrue();
        // pool
        var poolStatInfoGrain =
            Cluster.Client.GetGrain<IPoolStatInfoGrain>(AddVersionToKey(TradePairBtcUsdtAddress, _statInfoOptions.Value.DataVersion));
        var poolStatInfoResult = await poolStatInfoGrain.GetAsync();
        poolStatInfoResult.Success.ShouldBeTrue();
        poolStatInfoResult.Data.PairAddress.ShouldBe(TradePairBtcUsdtAddress);
        poolStatInfoResult.Data.TransactionCount.ShouldBe(0);
        poolStatInfoResult.Data.LastUpdateTime.ShouldBe(4000);
        poolStatInfoResult.Data.Tvl.ShouldBe(1100);
        poolStatInfoResult.Data.ValueLocked0.ShouldBe(1000);
        poolStatInfoResult.Data.ValueLocked1.ShouldBe(100);
        poolStatInfoResult.Data.Price.ShouldBe(0.1);

        var poolStatInfoIndex = await _poolStatInfoIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.PairAddress).Value(TradePairBtcUsdtAddress)) &&
            q.Term(i => i.Field(f => f.Version).Value(_statInfoOptions.Value.DataVersion)));
        poolStatInfoIndex.TransactionCount.ShouldBe(0);
        poolStatInfoIndex.PairAddress.ShouldBe(TradePairBtcUsdtAddress);
        poolStatInfoIndex.TradePair.Id.ShouldBe(TradePairBtcUsdtId);
        poolStatInfoIndex.LastUpdateTime.ShouldBe(4000);
        poolStatInfoIndex.Tvl.ShouldBe(1100);
        poolStatInfoIndex.ValueLocked0.ShouldBe(1000);
        poolStatInfoIndex.ValueLocked1.ShouldBe(100);
        poolStatInfoIndex.Price.ShouldBe(0.1);

       // token0
       var token0StatInfoGrain =
           Cluster.Client.GetGrain<ITokenStatInfoGrain>(AddVersionToKey("BTC", _statInfoOptions.Value.DataVersion));
       var token0StatInfoResult = await token0StatInfoGrain.GetAsync();
       token0StatInfoResult.Success.ShouldBeTrue();
       token0StatInfoResult.Data.Symbol.ShouldBe("BTC");
       token0StatInfoResult.Data.TransactionCount.ShouldBe(0);
       token0StatInfoResult.Data.LastUpdateTime.ShouldBe(4000);
       token0StatInfoResult.Data.Tvl.ShouldBe(1000);
       token0StatInfoResult.Data.PriceInUsd.ShouldBe(0);
       token0StatInfoResult.Data.PricePercentChange24h.ShouldBe(0);
        
       var token0StatInfoIndex = await _tokenStatInfoIndexRepository.GetAsync(q =>
           q.Term(i => i.Field(f => f.Symbol).Value("BTC")) &&
           q.Term(i => i.Field(f => f.Version).Value(_statInfoOptions.Value.DataVersion)));
       token0StatInfoIndex.Symbol.ShouldBe("BTC");
       token0StatInfoIndex.TransactionCount.ShouldBe(0);
       token0StatInfoIndex.LastUpdateTime.ShouldBe(4000);
       token0StatInfoIndex.Tvl.ShouldBe(1000);
       token0StatInfoIndex.PriceInUsd.ShouldBe(0);
       token0StatInfoIndex.PricePercentChange24h.ShouldBe(0);
       
       // token1
       var token1StatInfoGrain =
           Cluster.Client.GetGrain<ITokenStatInfoGrain>(AddVersionToKey("USDT", _statInfoOptions.Value.DataVersion));
       var token1StatInfoResult = await token1StatInfoGrain.GetAsync();
       token1StatInfoResult.Success.ShouldBeTrue();
       token1StatInfoResult.Data.Symbol.ShouldBe("USDT");
       token1StatInfoResult.Data.TransactionCount.ShouldBe(0);
       token1StatInfoResult.Data.LastUpdateTime.ShouldBe(4000);
       token1StatInfoResult.Data.Tvl.ShouldBe(100);
       token1StatInfoResult.Data.PriceInUsd.ShouldBe(0);
       token1StatInfoResult.Data.PricePercentChange24h.ShouldBe(0);
       
       var token1StatInfoIndex = await _tokenStatInfoIndexRepository.GetAsync(q =>
           q.Term(i => i.Field(f => f.Symbol).Value("USDT")) &&
           q.Term(i => i.Field(f => f.Version).Value(_statInfoOptions.Value.DataVersion)));
       token1StatInfoIndex.TransactionCount.ShouldBe(0);
       token1StatInfoIndex.Symbol.ShouldBe("USDT");
       token1StatInfoIndex.LastUpdateTime.ShouldBe(4000);
       token1StatInfoIndex.Tvl.ShouldBe(100);
       token1StatInfoIndex.PriceInUsd.ShouldBe(0);
       token1StatInfoIndex.PricePercentChange24h.ShouldBe(0);
       
       // global
       var globalGrain = Cluster.Client.GetGrain<IGlobalStatInfoGrain>(AddVersionToKey(syncRecordDto.ChainId, _statInfoOptions.Value.DataVersion));
       var globalResult = await globalGrain.GetAsync();
       globalResult.Data.Tvl.ShouldBe(1100);
    }

    [Fact]
    public async Task RefreshPoolAndTokenTaskTests()
    {
        await _statInfoInternalAppService.CreateSwapRecordAsync(new SwapRecordDto
        {
            ChainId = "tDVV",
            PairAddress = TradePairBtcUsdtAddress,
            Sender = "TV2aRV4W5oSJzxrkBvj8XmJKkMCiEQnAvLmtM9BqLTN3beXm2",
            TransactionHash = "6622966a928185655d691565d6128835e7d1ccdf1dd3b5f277c5f2a5b2802d37",
            Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.AddDays(-2)),
            AmountOut = NumberFormatter.WithDecimals(1000, 8),
            AmountIn = NumberFormatter.WithDecimals(100, 6),
            SymbolOut = TokenBtcSymbol,
            SymbolIn = TokenUsdtSymbol,
            TotalFee = 100,
            Channel = "test",
            BlockHeight = 99,
        }, _statInfoOptions.Value.DataVersion);
        await _statInfoInternalAppService.CreateSwapRecordAsync(new SwapRecordDto
        {
            ChainId = "tDVV",
            PairAddress = TradePairBtcUsdtAddress,
            Sender = "TV2aRV4W5oSJzxrkBvj8XmJKkMCiEQnAvLmtM9BqLTN3beXm2",
            TransactionHash = "6622966a928185655d691565d6128835e7d1ccdf1dd3b5f277c5f2a5b2802d38",
            Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.AddHours(-1)),
            AmountOut = NumberFormatter.WithDecimals(1000, 8),
            AmountIn = NumberFormatter.WithDecimals(100, 6),
            SymbolOut = TokenBtcSymbol,
            SymbolIn = TokenUsdtSymbol,
            TotalFee = 100,
            Channel = "test",
            BlockHeight = 99,
        }, _statInfoOptions.Value.DataVersion);
        await _statInfoInternalAppService.RefreshPoolStatInfoAsync("tDVV", _statInfoOptions.Value.DataVersion);
        var poolStatInfoGrain =
            Cluster.Client.GetGrain<IPoolStatInfoGrain>(AddVersionToKey(TradePairBtcUsdtAddress, _statInfoOptions.Value.DataVersion));
        var poolStatInfoResult = await poolStatInfoGrain.GetAsync();
        poolStatInfoResult.Data.VolumeInUsd7d.ShouldBe(2000);
        poolStatInfoResult.Data.VolumeInUsd24h.ShouldBe(1000);

        var poolStatInfoIndex = await _poolStatInfoIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.PairAddress).Value(TradePairBtcUsdtAddress)) &&
            q.Term(i => i.Field(f => f.Version).Value(_statInfoOptions.Value.DataVersion)));
        poolStatInfoIndex.VolumeInUsd7d.ShouldBe(2000);
        poolStatInfoIndex.VolumeInUsd24h.ShouldBe(1000);

        // token
        await _statInfoInternalAppService.RefreshTokenStatInfoAsync("tDVV", _statInfoOptions.Value.DataVersion);
        var btcStatInfoGrain =
            Cluster.Client.GetGrain<ITokenStatInfoGrain>(AddVersionToKey("BTC", _statInfoOptions.Value.DataVersion));
        var btcStatInfoResult = await btcStatInfoGrain.GetAsync();
        btcStatInfoResult.Data.VolumeInUsd24h.ShouldBe(1000);
        btcStatInfoResult.Data.PriceInUsd.ShouldBe(0);
        btcStatInfoResult.Data.PricePercentChange24h.ShouldBe(0);
        btcStatInfoResult.Data.PoolCount.ShouldBe(2);

        var usdtStatInfoGrain =
            Cluster.Client.GetGrain<ITokenStatInfoGrain>(AddVersionToKey("USDT", _statInfoOptions.Value.DataVersion));
        var usdtStatInfoResult = await usdtStatInfoGrain.GetAsync();
        usdtStatInfoResult.Data.VolumeInUsd24h.ShouldBe(100);
        usdtStatInfoResult.Data.PriceInUsd.ShouldBe(0);
        usdtStatInfoResult.Data.PricePercentChange24h.ShouldBe(0);
        usdtStatInfoResult.Data.PoolCount.ShouldBe(2);
        
        // global tvl
        await _statInfoInternalAppService.CreateSyncRecordAsync(new SyncRecordDto()
        {
            ChainId = "tDVV",
            PairAddress = TradePairBtcUsdtAddress,
            TransactionHash = "6622966a928185655d691565d6128835e7d1ccdf1dd3b5f277c5f2a5b2802d39",
            Timestamp = 4000,
            ReserveA = NumberFormatter.WithDecimals(1000, 8),
            ReserveB = NumberFormatter.WithDecimals(100, 6),
            SymbolA = TokenBtcSymbol,
            SymbolB = TokenUsdtSymbol,
            BlockHeight = 99,
        }, _statInfoOptions.Value.DataVersion);
        await _statInfoInternalAppService.RefreshTvlAsync("tDVV", _statInfoOptions.Value.DataVersion);
        var globalGrain = Cluster.Client.GetGrain<IGlobalStatInfoGrain>(AddVersionToKey("tDVV", _statInfoOptions.Value.DataVersion));
        var globalResult = await globalGrain.GetAsync();
        globalResult.Data.Tvl.ShouldBe(1100);

        await _statInfoInternalAppService.ClearOldTransactionHistoryAsync("tDVV", _statInfoOptions.Value.DataVersion);
    }
}