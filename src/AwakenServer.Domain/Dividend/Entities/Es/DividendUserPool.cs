using AElf.Indexing.Elasticsearch;

namespace AwakenServer.Dividend.Entities.Es
{
    public class DividendUserPool : DividendUserPoolBase, IIndexBuild
    {
        public DividendPoolBaseInfo PoolBaseInfo { get; set; }
    }
}