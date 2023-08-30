using System;
using AwakenServer.Entities;

namespace AwakenServer.Dividend.Entities
{
    public class DividendTokenBase : MultiChainEntity<Guid>
    {
        public long StartBlock { get; set; }
        public long EndBlock { get; set; }
        public string AmountPerBlock { get; set; }
    }
}