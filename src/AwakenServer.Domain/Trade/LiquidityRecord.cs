using System;

namespace AwakenServer.Trade
{
    public class LiquidityRecord : LiquidityRecordBase
    {
        public Guid TradePairId { get; set; }
        
        public LiquidityRecord()
        {
        }

        public LiquidityRecord(Guid id)
            : base(id)
        {
        }
    }
}