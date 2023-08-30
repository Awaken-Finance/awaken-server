using System;
using AwakenServer.Entities;
using Nest;

namespace AwakenServer.Debits.Entities
{
    public class CTokenUserInfoBase : MultiChainEntity<Guid>
    {
        [Keyword] public string User { get; set; }
        public bool IsEnteredMarket { get; set; }
        public string TotalBorrowAmount { get; set; }
        public string AccumulativeBorrowComp { get; set; }
        public string AccumulativeSupplyComp { get; set; }
    }
}