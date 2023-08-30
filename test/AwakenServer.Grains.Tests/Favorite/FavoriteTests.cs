using AwakenServer.Grains.Grain.Favorite;
using Shouldly;
using Xunit;

namespace AwakenServer.Grains.Tests.Favorite;

[Collection(ClusterCollection.Name)]
public class FavoriteTests : AwakenServerGrainTestBase
{
    private Guid TradePairId = Guid.NewGuid();
    private const string Address = "NewtAddress";
    
    [Fact]
    public async Task FavoriteGrainTest()
    {
        var dto = new FavoriteGrainDto
        {
            TradePairId = TradePairId,
            Address = Address,
            Timestamp = 0
        };
        var grain = Cluster.Client.GetGrain<IFavoriteGrain>(TradePairId + "-" + Address);
        //create
        var result = await grain.CreateAsync(dto);

        result.Success.ShouldBeTrue();
        result.Data.TradePairId.ShouldBe(TradePairId);
        result.Data.Address.ShouldBe(Address);

        result = await grain.CreateAsync(dto);
        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("Favorite already existed");
        
        //getList
        var list = await grain.GetListAsync();
        list.Success.ShouldBeTrue();
        list.Data.Count.ShouldBe(1);
        
        //delete
        result = await grain.DeleteAsync("-");
        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("Favorite not exist");

        result = await grain.DeleteAsync(TradePairId + "-" + Address);
        result.Success.ShouldBeTrue();
    }
}