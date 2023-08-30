using System;
using AwakenServer.Entities;
using Nest;

namespace AwakenServer.Dividend.Entities
{
    public class DividendUserPoolBase : MultiChainEntity<Guid>
    {
        [Keyword] public string User { get; set; }
        public string DepositAmount { get; set; }
    }
}