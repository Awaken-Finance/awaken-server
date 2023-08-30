using System;
using AwakenServer.Entities;
using Nest;

namespace AwakenServer.Dividend.Entities
{
    public class DividendUserTokenBase : MultiChainEntity<Guid>
    {
        [Keyword] public string User { get; set; }
        public string AccumulativeDividend { get; set; }
    }
}