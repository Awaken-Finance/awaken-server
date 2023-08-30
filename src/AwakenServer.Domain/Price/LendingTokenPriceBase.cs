using System;
using AwakenServer.Entities;
using Nest;

namespace AwakenServer.Price
{
    public class LendingTokenPriceBase: MultiChainEntity<Guid>
    {
        [Keyword]
        public string Price { get; set; }

        public double PriceValue { get; set; }
        public DateTime Timestamp { get; set; }
        public long BlockNumber { get; set; }
        
        protected LendingTokenPriceBase()
        {
        }
        
        protected LendingTokenPriceBase(Guid id) : base(id)
        {
        }
    }
}