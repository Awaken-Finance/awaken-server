using System.Threading.Tasks;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Shouldly;
using Xunit;

namespace AwakenServer.Grains.Tests.Trade;

[Collection(ClusterCollection.Name)]
public class UserLiquidityGrainTests : AwakenServerGrainTestBase
{
    [Fact(Timeout = 2000)]
    public async Task UserLiquidityGrainTest()
    {
        var tradePairGrain = Cluster.Client.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(TradePairEthUsdtId));
        await tradePairGrain.UpdatePriceAsync(new SyncRecordGrainDto
        {
            ChainId = ChainName,
            PairAddress = TradePairEthUsdtAddress,
            Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.Now),
            ReserveA = NumberFormatter.WithDecimals(1, 8),
            ReserveB = NumberFormatter.WithDecimals(1, 6),
            BlockHeight = 100,
            SymbolA = "ETH",
            SymbolB = "USDT"
        });
        
        var dto = new UserLiquidityGrainDto
        {
            TradePair = new TradePairWithTokenDto
            {
                Address = "0xPool006a6FaC8c710e53c4B2c2F96477119dA361",
                Id = TradePairEthUsdtId
            },
            ChainId = ChainId,
            Address = "BBB",
            LpTokenAmount = 45000,
            Type = LiquidityType.Mint
        };
        
        var grain = Cluster.Client.GetGrain<IUserLiquidityGrain>(GrainIdHelper.GenerateGrainId(dto.ChainId, dto.Address));
        
        //add
        var result = await grain.AddOrUpdateAsync(dto);
        
        result.Data.ChainId.ShouldBe(dto.ChainId);
        result.Data.TradePair.Id.ShouldBe(dto.TradePair.Id);
        result.Data.TradePair.Address.ShouldBe(dto.TradePair.Address);
        result.Data.Address.ShouldBe("BBB");
        result.Data.LpTokenAmount.ShouldBe(45000);
        
        var liquiditiesResult = await grain.GetAsync();
        var liquidities = liquiditiesResult.Data;
        liquidities.Count.ShouldBe(1);
        liquidities[0].Address.ShouldBe("BBB");
        liquidities[0].TradePair.Address.ShouldBe(dto.TradePair.Address);
        liquidities[0].LpTokenAmount.ShouldBe(45000);
        
        await grain.AddOrUpdateAsync(new UserLiquidityGrainDto
        {
            TradePair = new TradePairWithTokenDto
            {
                Address = "0xPool006a6FaC8c710e53c4B2c2F96477119dA361",
                Id = TradePairEthUsdtId
            },
            ChainId = ChainId,
            Address = "BBB",
            LpTokenAmount = 5000,
            Type = LiquidityType.Mint
        });
        
        liquidities = grain.GetAsync().Result.Data;
        liquidities.Count.ShouldBe(1);
        liquidities[0].LpTokenAmount.ShouldBe(50000);
        
        var userAssetResult = await grain.GetAssetAsync();
        var userAsset = userAssetResult.Data.AssetUSD * Math.Pow(10, 8);
        userAsset.ShouldBe(1);
    }
}