using System;
using AElf.Indexing.Elasticsearch;

namespace AwakenServer.Trade.Index
{
    public class TradeRecord : TradeRecordBase, IIndexBuild
    {
        public TradePairWithToken TradePair { get; set; }
        
        public TradeRecord()
        {
        }

        public TradeRecord(Guid id)
            : base(id)
        {
        }
    }
}