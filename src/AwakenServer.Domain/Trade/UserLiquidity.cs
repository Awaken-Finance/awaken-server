using System;

namespace AwakenServer.Trade
{
    public class UserLiquidity : UserLiquidityBase
    {
        public Guid TradePairId { get; set; }
        
        public UserLiquidity()
        {
        }

        public UserLiquidity(Guid id)
            : base(id)
        {
        }
    }
}