using System;
using AwakenServer.Entities;
using Nest;

namespace AwakenServer.Dividend.Entities
{
    public class DividendUserRecordBase : MultiChainEntity<Guid>
    {
        public string TransactionHash { get; set; }
        [Keyword] public string User { get; set; }
        public DateTime DateTime { get; set; }
        public string Amount { get; set; }

        public BehaviorType BehaviorType { get; set; }
        // public decimal TokenUsdtPrice { get; set; }
    }
}