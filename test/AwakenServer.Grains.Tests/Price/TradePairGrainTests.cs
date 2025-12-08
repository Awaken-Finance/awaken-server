using System;
using System.Threading.Tasks;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.Price.TradeRecord;
using AwakenServer.Grains.Grain.Tokens.TokenPrice;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Shouldly;
using Xunit;

namespace AwakenServer.Grains.Tests.Price;

[Collection(ClusterCollection.Name)]
public class TradePairGrainTests : AwakenServerGrainTestBase
{
    [Fact]
    public async Task SyncTest()
    {
        var tradePairId = Guid.Parse("feb4d613-1f3b-451e-9961-5043760ba295");
        var tradePair = new TradePairGrainDto
        {
            Id = tradePairId
        };
        
        var grain = Cluster.Client.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(tradePairId));
        
        //get
        var result = await grain.GetAsync();
        result.Success.ShouldBe(false);
        
        //add
        await grain.AddOrUpdateAsync(tradePair);
        
        //get
        result = await grain.GetAsync();
        Assert.NotNull(result);
        result.Data.Id.ShouldBe(tradePairId);
        result.Data.Price.ShouldBe(0.0);
        result.Data.TotalSupply.ShouldBe(null);
        
        //update
        tradePair.Price = 1.1;
        tradePair.TotalSupply = "22.2";
        await grain.AddOrUpdateAsync(tradePair);
        result = await grain.GetAsync();
        result.Data.Price.ShouldBe(1.1);
        result.Data.TotalSupply.ShouldBe("22.2");
    }

    [Fact]
    public async Task UpdateLiquidityTest()
    {
        var grain = Cluster.Client.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(TradePairEthUsdtId));
        var result = await grain.UpdatePriceAsync(new SyncRecordGrainDto
        {
            ChainId = ChainName,
            PairAddress = TradePairEthUsdtAddress,
            Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.Now),
            ReserveA = 100000000,
            ReserveB = 1000000,
            BlockHeight = 100,
            SymbolA = "ETH",
            SymbolB = "USDT",
            Token0PriceInUsd = 1,
            Token1PriceInUsd = 1
        });
        
        result.Success.ShouldBe(true);
        result.Data.TradePairDto.Price.ShouldBe(1);
        result.Data.TradePairDto.TVL.ShouldBe(2);
        result.Data.TradePairDto.PriceUSD.ShouldBe(1);
    }
    
    [Fact]
    public async Task UpdateTotalSupplyAsync()
    {
        var grain = Cluster.Client.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(TradePairEthUsdtId));
        var result = await grain.UpdateTotalSupplyAsync(new LiquidityRecordGrainDto
        {
            ChainId = ChainName,
            Timestamp = DateTime.Now,
            Type = LiquidityType.Mint,
            LpTokenAmount = "100000",
        });
        
        result.Success.ShouldBe(true);
        result.Data.TradePairDto.TotalSupply.ShouldBe("200000");
    }
    
    [Fact]
    public async Task UpdateTradeRecordAsync()
    {
        var grain = Cluster.Client.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(TradePairEthUsdtId));
        var result = await grain.UpdateTradeRecordAsync(new TradeRecordGrainDto
        {
            ChainId = ChainName,
            Timestamp = DateTime.Now,
            Token0Amount = "10",
            Token1Amount = "100",
        }, 1);
        
        result.Success.ShouldBe(true);
        result.Data.TradePairDto.Volume24h.ShouldBe(10);
        result.Data.TradePairDto.TradeValue24h.ShouldBe(100);
        result.Data.TradePairDto.TradeCount24h.ShouldBe(1);
        
        result = await grain.UpdateTradeRecordAsync(new TradeRecordGrainDto
        {
            ChainId = ChainName,
            Timestamp = DateTime.Now,
            Token0Amount = "10",
            Token1Amount = "100",
        }, 2);
        result.Success.ShouldBe(true);
        result.Data.TradePairDto.Volume24h.ShouldBe(20);
        result.Data.TradePairDto.TradeValue24h.ShouldBe(200);
        result.Data.TradePairDto.TradeCount24h.ShouldBe(2);
    }
    
    [Fact]
    public async Task PercentChange24hAsync()
    {
        var grain = Cluster.Client.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(TradePairEthUsdtId));
        await grain.UpdatePriceAsync(new SyncRecordGrainDto
        {
            ChainId = ChainName,
            PairAddress = TradePairEthUsdtAddress,
            Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.Now.AddHours(-2)),
            ReserveA = 200000000,
            ReserveB = 2000000,
            BlockHeight = 100,
            SymbolA = TokenEthSymbol,
            SymbolB = TokenUsdtSymbol,
            Token0PriceInUsd = 1,
            Token1PriceInUsd = 1
        });
        
        await grain.UpdatePriceAsync(new SyncRecordGrainDto
        {
            ChainId = ChainName,
            PairAddress = TradePairEthUsdtAddress,
            Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.Now),
            ReserveA = 100000000,
            ReserveB = 1000000,
            BlockHeight = 100,
            SymbolA = TokenEthSymbol,
            SymbolB = TokenUsdtSymbol,
            Token0PriceInUsd = 1,
            Token1PriceInUsd = 1
        });
        
        var result = await grain.UpdateAsync(DateTime.Now, 0, "", 1.0, 1.0);
        result.Data.TVLPercentChange24h.ShouldBe(-50);
    }
    
    
    [Fact]
    public async Task LatestSnapshotAsyncTest()
    {
        var grain = Cluster.Client.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(TradePairEthUsdtId));
        await grain.AddOrUpdateSnapshotAsync(new TradePairMarketDataSnapshotGrainDto()
        {
            ChainId = ChainName,
            TotalSupply = "100000",
            Timestamp = DateTime.Now.AddDays(-10)
        });

        await grain.UpdateTotalSupplyAsync(new LiquidityRecordGrainDto()
        {
            ChainId = ChainName,
            Pair = TradePairEthUsdtAddress,
            Type = LiquidityType.Mint,
            Timestamp = DateTime.Now,
            LpTokenAmount = "100000"
        });

        var result = await grain.GetAsync();
        result.Data.TotalSupply.ShouldBe("200000");
    }
}