using AwakenServer.Grains.Grain.Asset;
using Shouldly;
using Xunit;

namespace AwakenServer.Grains.Tests.Asset;

[Collection(ClusterCollection.Name)]
public class AssetTests : AwakenServerGrainTestBase
{
    
    [Fact]
    public async Task DefaultTokenGrainTest()
    {
        var grain = Cluster.Client.GetGrain<IDefaultTokenGrain>("TEST_ADDRESS");
        var result = await grain.SetTokenAsync("BTC");
        result.Success.ShouldBeTrue();


        var defaultToken = await grain.GetAsync();
        defaultToken.Data.TokenSymbol.ShouldBe("BTC");


        result = await grain.SetTokenAsync("ETH");
        result.Success.ShouldBeTrue();


        defaultToken = await grain.GetAsync();
        defaultToken.Data.TokenSymbol.ShouldBe("ETH");
    }
}