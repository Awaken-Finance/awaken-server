using AElf.Indexing.Elasticsearch;
using AwakenServer.Tokens;

namespace AwakenServer.Dividend.Entities.Es
{
    public class DividendUserToken : DividendUserTokenBase, IIndexBuild
    {
        public Token DividendToken { get; set; }
        public DividendPoolBaseInfo PoolBaseInfo { get; set; }
    }
}