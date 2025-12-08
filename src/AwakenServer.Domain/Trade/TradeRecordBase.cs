using System;

namespace AwakenServer.Trade
{
    public abstract class TradeRecordBase : UserRecordBase
    {
        public double Price { get; set; }
        public TradeSide Side { get; set; }

        protected TradeRecordBase()
        {
        }

        protected TradeRecordBase(Guid id)
            : base(id)
        {
        }
    }
}