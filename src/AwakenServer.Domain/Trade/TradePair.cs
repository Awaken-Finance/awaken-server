using System;

namespace AwakenServer.Trade
{
    public class TradePair : TradePairBase
    {
        public Guid Token0Id { get; set; }
        public Guid Token1Id { get; set; }
        
        public TradePair()
        {
        }

        public TradePair(Guid id)
            : base(id)
        {
        }
    }
}