using System;

namespace AwakenServer.Trade
{
    public class TradeRecord:TradeRecordBase
    {
        public Guid TradePairId { get; set; }
        
        public TradeRecord()
        {
        }

        public TradeRecord(Guid id)
            : base(id)
        {
        }
    }
}