using System;
using AwakenServer.Entities;
using Nest;

namespace AwakenServer.Farms.Entities
{
    public abstract class FarmUserInfoBase : MultiChainEntity<Guid>
    {
        [Keyword] public string User { get; set; }
        public string CurrentDepositAmount { get; set; } = "0";
        public string AccumulativeDividendProjectTokenAmount { get; set; } = "0";
        public string AccumulativeDividendUsdtAmount { get; set; } = "0";
    }
}