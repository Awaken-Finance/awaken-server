using System;
using AwakenServer.Entities;
using Nest;

namespace AwakenServer.Farms.Entities
{
    public class FarmPoolBase : MultiChainEntity<Guid>
    {
        [Keyword] public override Guid Id { get; set; }
        [Keyword] public Guid FarmId { get; set; }
        public int Pid { get; set; }
        public PoolType PoolType { get; set; }
    }

    public class EditableStateFarmPool : FarmPoolBase
    {
        public int Weight { get; set; }
        public long LastUpdateBlockHeight { get; set; }
        public string TotalDepositAmount { get; set; } = "0";
        public string AccumulativeDividendProjectToken { get; set; } = "0";
        public string AccumulativeDividendUsdt { get; set; } = "0";
    }
}