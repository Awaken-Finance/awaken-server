using System;
using AwakenServer.Entities;

namespace AwakenServer.Trade
{
    public class UserTradeSummary : MultiChainEntity<Guid>
    {
        public Guid TradePairId { get; set; }
        public string Address { get; set; }
        public DateTime LatestTradeTime { get; set; }

        protected UserTradeSummary()
        {
        }

        protected UserTradeSummary(Guid id)
            : base(id)
        {
        }
    }
}