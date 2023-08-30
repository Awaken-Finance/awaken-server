using System;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Tokens;

namespace AwakenServer.Price.Index
{
    public class OtherLpToken : OtherLpTokenBase, IIndexBuild
    {
        public Token Token0 { get; set; }

        public Token Token1 { get; set; }

        public OtherLpToken()
        {

        }

        public OtherLpToken(Guid id) : base(id)
        {

        }
    }
}