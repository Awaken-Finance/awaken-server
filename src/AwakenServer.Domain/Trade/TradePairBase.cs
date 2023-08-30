using System;
using AwakenServer.Entities;
using Nest;

namespace AwakenServer.Trade
{
    public abstract class TradePairBase : MultiChainEntity<Guid>
    {
        [Keyword]
        public string Address { get; set; }
        public double FeeRate { get; set; }
        public bool IsTokenReversed { get; set; }

        protected TradePairBase()
        {
        }

        protected TradePairBase(Guid id)
            : base(id)
        {
        }
    }
}