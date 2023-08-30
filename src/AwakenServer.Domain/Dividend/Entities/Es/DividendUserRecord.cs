using AElf.Indexing.Elasticsearch;
using AwakenServer.Tokens;

namespace AwakenServer.Dividend.Entities.Es
{
    public class DividendUserRecord : DividendUserRecordBase, IIndexBuild
    {
        public DividendPoolBaseInfo PoolBaseInfo { get; set; }
        public Token DividendToken { get; set; }
    }
}