using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace AwakenServer.Farms.Entities.Es
{

    public class FarmUserInfo : FarmUserInfoBase, IIndexBuild
    {
        [Keyword] public override Guid Id { get; set; }
        public FarmBase FarmInfo { get; set; }
        public FarmPoolBase PoolInfo { get; set; }
        public Tokens.Token SwapToken { get; set; }
        public Tokens.Token Token1 { get; set; }
        public Tokens.Token Token2 { get; set; }
    }
}