using Orleans.TestingHost;

namespace AwakenServer.Trade.Ethereum
{
    public class TradeProcessorTestBase: AwakenServerTestBase<TradeProcessorTestModule>
    {
        protected readonly TestCluster Cluster;

        public TradeProcessorTestBase()
        {
            Cluster = GetRequiredService<ClusterFixture>().Cluster;
        }
    }
}