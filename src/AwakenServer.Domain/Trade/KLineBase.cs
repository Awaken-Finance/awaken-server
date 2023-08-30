using System;
using AwakenServer.Entities;
using Nest;

namespace AwakenServer.Trade
{
    public class KLineBase : MultiChainEntity<Guid>
    {
        [Keyword]
        public Guid TradePairId { get; set; }
        public int Period { get; set; }
        public double Open { get; set; }
        public double Close { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Volume { get; set; }
        public long Timestamp { get; set; }

        protected KLineBase()
        {
        }

        protected KLineBase(Guid id)
            : base(id)
        {
        }
    }
}