using System;
using AElf.Indexing.Elasticsearch;

namespace AwakenServer.Trade.Index
{
    public class UserLiquidity: UserLiquidityBase, IIndexBuild
    {
        public TradePairWithToken TradePair { get; set; }
        
        public UserLiquidity()
        {
        }

        public UserLiquidity(Guid id)
            : base(id)
        {
        }
    }
}