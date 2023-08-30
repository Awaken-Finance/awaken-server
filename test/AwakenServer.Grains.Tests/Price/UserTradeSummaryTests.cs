using AwakenServer.Grains.Grain.Price.UserTradeSummary;
using Shouldly;
using Xunit;

namespace AwakenServer.Grains.Tests.Price;

[Collection(ClusterCollection.Name)]
public class UserTradeSummaryTests : AwakenServerGrainTestBase
{
    [Fact]
    public async Task UserTradeSummaryGrainTest()
    {
        var timestamp = DateTime.Now;
        var dto = new UserTradeSummaryGrainDto
        {
            Id = Guid.NewGuid(),
            ChainId = Guid.Parse("5ccf55f5-4d36-404d-8921-9c3fcd07a281").ToString(),
            TradePairId = Guid.Parse("feb4d613-1f3b-451e-9961-5043760ba295"),
            Address = "DefaultAddress",
            LatestTradeTime = timestamp
        };
        var grain = Cluster.Client.GetGrain<IUserTradeSummaryGrain>($"{dto.ChainId}-{dto.TradePairId}-{dto.Address}");
        //get
        var result = await grain.GetAsync();
        result.Success.ShouldBeFalse();
        
        //add
        result = await grain.AddOrUpdateAsync(dto);

        result.Success.ShouldBeTrue();
        result.Data.ChainId.ShouldBe(dto.ChainId);
        result.Data.TradePairId.ShouldBe(dto.TradePairId);
        result.Data.Address.ShouldBe("DefaultAddress");
        var data = result.Data;

        //update
        data.LatestTradeTime = timestamp.AddSeconds(10);
        result = await grain.AddOrUpdateAsync(data);
        result.Success.ShouldBeTrue();
        result.Data.Id.ShouldBe(dto.Id);
        result.Data.LatestTradeTime.ShouldBe(timestamp.AddSeconds(10));

        //get
        result = await grain.GetAsync();
        result.Success.ShouldBeTrue();
        result.Data.Id.ShouldBe(dto.Id);
        result.Data.ChainId.ShouldBe(dto.ChainId);
        result.Data.TradePairId.ShouldBe(dto.TradePairId);
        result.Data.LatestTradeTime.ShouldBe(timestamp.AddSeconds(10));
    }
}