using System;
using AwakenServer.Entities;

namespace AwakenServer.Dividend.Entities
{
    public class DividendPoolTokenBase : MultiChainEntity<Guid>
    {
        public string AccumulativeDividend { get; set; }
        public long LastRewardBlock { get; set; }
    }
}