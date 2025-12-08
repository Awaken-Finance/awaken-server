using System;
using AElf.Indexing.Elasticsearch;

namespace AwakenServer.Trade.Index
{
    public class TradeRecord : TradeRecordBase, IIndexBuild
    {
        public TradePairWithToken TradePair { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsSubRecord { get; set; } = false;
        
        public TradeRecord()
        {
        }

        public TradeRecord(Guid id)
            : base(id)
        {
        }
    }
}