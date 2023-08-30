using System;
using AwakenServer.Entities;
using Nest;

namespace AwakenServer.Farms.Entities
{
    public class FarmBase : MultiChainEntity<Guid>
    {
        [Keyword] public override Guid Id { get; set; }
        [Keyword] public string FarmAddress { get; set; }
        public FarmType FarmType { get; set; }
    }

    public class EditableStateFarm : FarmBase
    {
        public long StartBlock { get; set; }
        public long MiningHalvingPeriod1 { get; set; }
        public long MiningHalvingPeriod2 { get; set; }
        public long UsdtDividendStartBlockHeight { get; set; }
        public long UsdtDividendEndBlockHeight { get; set; }
        public string UsdtDividendPerBlock { get; set; } = "0";
        public string ProjectTokenMinePerBlock1 { get; set; } = "0";
        public string ProjectTokenMinePerBlock2 { get; set; } = "0";
        public int TotalWeight { get; set; }
    }
}