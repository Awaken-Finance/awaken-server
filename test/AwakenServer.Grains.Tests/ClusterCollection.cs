using Xunit;

namespace AwakenServer.Grains.Tests;

[CollectionDefinition(ClusterCollection.Name)]
public class ClusterCollection:ICollectionFixture<ClusterFixture>
{
    public const string Name = "ClusterCollection";
}