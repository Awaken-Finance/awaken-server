using Orleans.TestingHost;

namespace AwakenServer.Grains.Tests;

public class AwakenServerGrainTestBase : AwakenServerTestBase<AwakenServerGrainTestModule>
{
    protected readonly TestCluster Cluster;

    public AwakenServerGrainTestBase()
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
    }
}