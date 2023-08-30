using System;
using AElf.Indexing.Elasticsearch;

namespace AwakenServer.Price.Index
{
    public class LendingTokenPrice : LendingTokenPriceIndexBase, IIndexBuild
    {
        public LendingTokenPrice()
        {

        }

        public LendingTokenPrice(Guid id) : base(id)
        {

        }
    }
}