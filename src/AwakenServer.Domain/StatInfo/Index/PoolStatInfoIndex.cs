using AElf.Indexing.Elasticsearch;
using AwakenServer.Trade.Index;

namespace AwakenServer.StatInfo.Index;

public class PoolStatInfoIndex : PoolStatInfo, IIndexBuild
{
    public TradePairWithToken TradePair { get; set; }
    public PoolStatInfoIndex()
    {
    }
}