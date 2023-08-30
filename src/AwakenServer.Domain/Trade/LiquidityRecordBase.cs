
using System;
using Nest;

namespace AwakenServer.Trade
{
    public class LiquidityRecordBase : UserRecordBase
    {
        public LiquidityType Type { get; set; }
        [Keyword]
        public string LpTokenAmount { get; set; }
        
        protected LiquidityRecordBase()
        {
        }

        protected LiquidityRecordBase(Guid id)
            : base(id)
        {
        }
    }
}