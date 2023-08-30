using System;
using System.Collections.Generic;
using AwakenServer.Farms.Entities.Es;

namespace AwakenServer.Farms.Options
{
    public class FarmOption
    {
        public bool IsResetData { get; set; }
        public List<FarmConfig> Farms { get; set; }

        public int LastTerm { get; set; } = 4;
        public string MainToken { get; set; } = "AWKN";
        public string BtcSymbol { get; set; } = "BTC";
    }

    public class FarmConfig
    {
        public Guid Id { get; set; }
        public string ChainId { get; set; }
        public string FarmAddress { get; set; }
        public FarmType FarmType { get; set; }
        public long StartBlock { get; set; }
        public int TotalWeight { get; set; }
        public long MiningHalvingPeriod1 { get; set; }
        public long MiningHalvingPeriod2 { get; set; }
        public string ProjectTokenMinePerBlock1 { get; set; }
        public string ProjectTokenMinePerBlock2 { get; set; }
        public string UsdtDividendPerBlock { get; set; }
        public long UsdtDividendEndBlockHeight { get; set; }
        public long UsdtDividendStartBlockHeight { get; set; }

        public Farm GetFarm()
        {
            return new (Id)
            {

                ChainId = ChainId,
                FarmAddress = FarmAddress,
                FarmType = FarmType,
                StartBlock = StartBlock,
                TotalWeight = TotalWeight,
                MiningHalvingPeriod1 = MiningHalvingPeriod1,
                MiningHalvingPeriod2 = MiningHalvingPeriod2,
                ProjectTokenMinePerBlock1 = ProjectTokenMinePerBlock1,
                ProjectTokenMinePerBlock2 = ProjectTokenMinePerBlock2,
                UsdtDividendPerBlock = UsdtDividendPerBlock,
                UsdtDividendEndBlockHeight = UsdtDividendEndBlockHeight,
                UsdtDividendStartBlockHeight = UsdtDividendStartBlockHeight
            };
        }
    }
}