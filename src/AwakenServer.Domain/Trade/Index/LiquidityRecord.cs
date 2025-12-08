using System;
using AElf.Indexing.Elasticsearch;

namespace AwakenServer.Trade.Index
{
    public class LiquidityRecord : LiquidityRecordBase, IIndexBuild
    {
        public TradePairWithToken TradePair { get; set; }
        
        public LiquidityRecord()
        {
        }

        public LiquidityRecord(Guid id)
            : base(id)
        {
        }
    }
}