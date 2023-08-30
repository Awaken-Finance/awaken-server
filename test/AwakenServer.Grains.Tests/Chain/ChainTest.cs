using AwakenServer.Grains.Grain.Chain;
using Shouldly;
using Xunit;

namespace AwakenServer.Grains.Tests.Chain;

[Collection(ClusterCollection.Name)]
public class ChainTest : AwakenServerGrainTestBase
{
    [Fact]
    public async Task AddChainTest()
    {
        var id = Guid.NewGuid().ToString();
        var chain = new ChainGrainDto()
        {
            Id = id,
            Name = "AELF",
            AElfChainId = 0
        };
        var grain = Cluster.Client.GetGrain<IChainGrain>(id);
        var result = await grain.AddChainAsync(chain);
        
        result.Success.ShouldBeTrue();
        result.Data.Id.ShouldBe(id);
        
        result = await grain.AddChainAsync(new ChainGrainDto());
        result.Success.ShouldBeFalse();
        
        var grain1 = Cluster.Client.GetGrain<IChainGrain>(Guid.Empty.ToString());
        var emptyResult = await grain1.AddChainAsync(new ChainGrainDto());
        emptyResult.Success.ShouldBeFalse();
    }
    
    [Fact]
    public async Task GetChainTest()
    {
        var id = Guid.NewGuid().ToString();
        var chain = new ChainGrainDto()
        {
            Id = id,
            Name = "AELF",
            AElfChainId = 0
        };
        var grain = Cluster.Client.GetGrain<IChainGrain>(id);
        var result = await grain.AddChainAsync(chain);
        
        result.Success.ShouldBeTrue();
        result.Data.Id.ShouldBe(id);
        
        var grain1 = Cluster.Client.GetGrain<IChainGrain>(id);
        await grain1.SetBlockHeightAsync(100,1000);
        await grain1.SetNameAsync("xxx");
        await grain1.SetChainIdAsync(10);
        var getResult = await grain1.GetByIdAsync(id);
        getResult.ShouldNotBeNull();
        getResult.Id.ShouldBe(id);
        getResult.LatestBlockHeight.ShouldBe(100);
        getResult.Name.ShouldBe("xxx");
        getResult.AElfChainId.ShouldBe(10);
        
        await grain1.SetBlockHeightAsync(99,1001);
        getResult = await grain1.GetByIdAsync(id);
        getResult.LatestBlockHeight.ShouldBe(100);
        
        getResult = await grain1.GetByIdAsync("");
        getResult.ShouldBeNull();
    }
}