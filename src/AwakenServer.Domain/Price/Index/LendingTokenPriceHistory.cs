using System;
using AElf.Indexing.Elasticsearch;

namespace AwakenServer.Price.Index
{
    public class LendingTokenPriceHistory : LendingTokenPriceIndexBase, IIndexBuild
    {
        public DateTime UpdateTimestamp { get; set; }
        public LendingTokenPriceHistory()
        {

        }

        public LendingTokenPriceHistory(Guid id) : base(id)
        {

        }
    }
}