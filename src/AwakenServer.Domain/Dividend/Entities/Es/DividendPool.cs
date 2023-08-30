using AElf.Indexing.Elasticsearch;
using AwakenServer.Tokens;

namespace AwakenServer.Dividend.Entities.Es
{
    public class DividendPoolBaseInfo : DividendPoolBase
    {
        public DividendBase Dividend { get; set; }
        public Token PoolToken { get; set; }
    }

    public class DividendPool : EditableDividendPoolBase, IIndexBuild
    {
        public DividendBase Dividend { get; set; }
        public Token PoolToken { get; set; }
    }
}