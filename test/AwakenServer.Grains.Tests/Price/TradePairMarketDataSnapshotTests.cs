using AwakenServer.Grains.Grain.Price.TradePair;
using Shouldly;
using Xunit;

namespace AwakenServer.Grains.Tests.Price;

[Collection(ClusterCollection.Name)]
public class TradePairMarketDataSnapshotTests : AwakenServerGrainTestBase
{
    [Fact]
    public async Task TradePairMarketDataSnapshotGrainTest()
    {
        var timestamp = DateTime.Now;
        var dto = new TradePairMarketDataSnapshotGrainDto
        {
            ChainId = Guid.Parse("5ccf55f5-4d36-404d-8921-9c3fcd07a281").ToString(),
            TradePairId = Guid.Parse("feb4d613-1f3b-451e-9961-5043760ba295"),
            Timestamp = timestamp,
            TotalSupply = "10.1"
        };

        var grain = Cluster.Client.GetGrain<ITradePairMarketDataSnapshotGrain>($"{dto.ChainId}-{dto.TradePairId}-{(long)(timestamp - DateTime.UnixEpoch).TotalMilliseconds}");
        
        //get
        var result = await grain.GetAsync();
        result.Success.ShouldBeFalse();
        
        //add
        result = await grain.AddOrUpdateAsync(dto);
        result.Success.ShouldBeTrue();
        result.Data.ChainId.ShouldBe(dto.ChainId);
        result.Data.TradePairId.ShouldBe(dto.TradePairId);
        result.Data.Timestamp.ShouldBe(timestamp);
        result.Data.TotalSupply.ShouldBe("10.1");

        //get
        result = await grain.GetAsync();
        result.Data.ChainId.ShouldBe(dto.ChainId);
        result.Data.TradePairId.ShouldBe(dto.TradePairId);
        result.Data.Timestamp.ShouldBe(timestamp);
        result.Data.TotalSupply.ShouldBe("10.1");
        
        //update
        dto = result.Data;
        dto.TotalSupply = "22.2";
        await grain.AddOrUpdateAsync(dto);
        result = await grain.GetAsync();
        result.Data.Id.ShouldBe(dto.Id);
        result.Data.TotalSupply.ShouldBe("22.2");
    }
}