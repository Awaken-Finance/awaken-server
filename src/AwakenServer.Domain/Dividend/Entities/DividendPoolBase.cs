using System;
using AwakenServer.Entities;
using Nest;

namespace AwakenServer.Dividend.Entities
{
    public class DividendPoolBase : MultiChainEntity<Guid>
    {
        [Keyword] public override Guid Id { get; set; }
        public int Pid { get; set; }
    }

    public class EditableDividendPoolBase : DividendPoolBase
    {
        public int Weight { get; set; }
        public string DepositAmount { get; set; }
    }
}