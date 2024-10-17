using System;
using AwakenServer.Entities;
using Nest;
using Orleans;

namespace AwakenServer.Trade
{
    [GenerateSerializer]
    public abstract class TradePairBase : MultiChainEntity<Guid>
    {
        [Keyword]
        [Id(0)] public string Address { get; set; }
        [Id(1)] public double FeeRate { get; set; }
        [Id(2)] public bool IsTokenReversed { get; set; }

        protected TradePairBase()
        {
        }

        protected TradePairBase(Guid id)
            : base(id)
        {
        }
    }
}