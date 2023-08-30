using AElf.Indexing.Elasticsearch;
using AwakenServer.Tokens;

namespace AwakenServer.Dividend.Entities.Es
{
    public class DividendPoolToken : DividendPoolTokenBase, IIndexBuild
    {
        public DividendPoolBaseInfo PoolBaseInfo { get; set; }
        public Token DividendToken { get; set; }
    }
}