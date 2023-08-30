using System;

namespace AwakenServer.Price
{
    public class LendingTokenPrice : LendingTokenPriceBase
    {
        public Guid TokenId { get; set; }
        
        public LendingTokenPrice()
        {
        }
        
        public LendingTokenPrice(Guid id) : base(id)
        {
        }
    }
}