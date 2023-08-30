using System;
using AwakenServer.Entities;
using Nest;

namespace AwakenServer.Trade
{
    public abstract class UserLiquidityBase : MultiChainEntity<Guid>
    {
        [Keyword]
        public string Address { get; set; }
        [Keyword]
        public string LpTokenAmount { get; set; }
        
        protected UserLiquidityBase()
        {
        }

        protected UserLiquidityBase(Guid id)
            : base(id)
        {
        }
    }
}