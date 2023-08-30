using System;
using AElf.Indexing.Elasticsearch;

namespace AwakenServer.Trade.Index
{
    public class TradePairMarketDataSnapshot : TradePairMarketDataBase, IIndexBuild
    {
        public TradePairMarketDataSnapshot()
        {
        }

        public TradePairMarketDataSnapshot(Guid id)
            : base(id)
        {
        }
    }
}