using AElf.Indexing.Elasticsearch;

namespace AwakenServer.StatInfo.Index;

public class TransactionHistoryIndex : TransactionHistory, IIndexBuild
{
    public TransactionHistoryIndex()
    {
    }
}