using AElf.Indexing.Elasticsearch;

namespace AwakenServer.StatInfo.Index;

public class StatInfoSnapshotIndex : StatInfoSnapshot, IIndexBuild
{
    public StatInfoSnapshotIndex()
    {
    }
}