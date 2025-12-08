using AwakenServer.Grains.Grain.ApplicationHandler;
using Shouldly;
using Xunit;

namespace AwakenServer.Grains.Tests.Trade;

[Collection(ClusterCollection.Name)]
public class ContractServiceGraphQLGrainTests : AwakenServerGrainTestBase
{
    [Fact]
    public async Task ContractServiceGraphQLTest()
    {
        var grainKey = "UpdateType" + "tDVV";
        var grain = Cluster.Client.GetGrain<IContractServiceGraphQLGrain>(grainKey);
        await grain.SetStateAsync(10);

        var result = await grain.GetStateAsync();
        result.ShouldBe(10);
    }
}