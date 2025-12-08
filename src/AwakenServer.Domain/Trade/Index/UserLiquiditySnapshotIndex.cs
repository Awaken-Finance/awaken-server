using AElf.Indexing.Elasticsearch;

namespace AwakenServer.Trade.Index;

public class UserLiquiditySnapshotIndex : UserLiquiditySnapshot, IIndexBuild
{
    public UserLiquiditySnapshotIndex()
    {
    }
}