using AElf.Indexing.Elasticsearch;
using AwakenServer.Tokens;

namespace AwakenServer.Dividend.Entities.Es
{
    public class DividendToken : DividendTokenBase, IIndexBuild
    {
        public DividendBase Dividend { get; set; }
        public Token Token { get; set; }
    }
}