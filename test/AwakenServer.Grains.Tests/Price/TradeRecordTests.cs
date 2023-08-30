using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.Price.TradeRecord;
using AwakenServer.Trade;
using Shouldly;
using Xunit;

namespace AwakenServer.Grains.Tests.Price;

[Collection(ClusterCollection.Name)]
public class TradeRecordTests : AwakenServerGrainTestBase
{
    [Fact]
    public async Task TradeRecordGrainTest()
    {
        var dto = new TradeRecordGrainDto
        {
            Id = Guid.NewGuid(),
            ChainId = Guid.Parse("b16b940b-986b-4610-b344-c3d71b843288").ToString(),
            TradePairId = Guid.Parse("9846eef4-661e-4deb-a7cd-66059eb0ae82"),
            Address = "DefaultAddress",
            Side = TradeSide.Sell,
            Token0Amount = "10",
            Token1Amount = "20",
            Timestamp = DateTime.Now,
            Price = 15.5
        };
        var grain = Cluster.Client.GetGrain<ITradeRecordGrain>(dto.Id);
        //insert
        var result = await grain.InsertAsync(dto);

        result.Success.ShouldBeTrue();
        result.Data.ChainId.ShouldBe(dto.ChainId);
        result.Data.TradePairId.ShouldBe(dto.TradePairId);
        result.Data.Address.ShouldBe(dto.Address);
        result.Data.Token0Amount.ShouldBe(dto.Token0Amount);
        result.Data.Token1Amount.ShouldBe(dto.Token1Amount);
        result.Data.Timestamp.ShouldBe(dto.Timestamp);;
        result.Data.Price.ShouldBe(15.5);
        
        result = await grain.GetAsync();
        result.Data.Id.ShouldBe(dto.Id);
    }
}