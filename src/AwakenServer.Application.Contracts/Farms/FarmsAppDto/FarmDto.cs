using System;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Farms
{
    public class FarmDto : EntityDto<Guid>
    {
        public string ChainId { get; set; }
        public string FarmAddress { get; set; }
        public FarmType FarmType { get; set; }
        public long StartBlock { get; set; }
        public long MiningHalvingPeriod1 { get; set; }
        public long MiningHalvingPeriod2 { get; set; }
        public long UsdtDividendStartBlockHeight { get; set; }
        public long UsdtDividendEndBlockHeight { get; set; }
        public string UsdtDividendPerBlock { get; set; }
        public string ProjectTokenMinePerBlock1 { get; set; }
        public string ProjectTokenMinePerBlock2 { get; set; }
        public int TotalWeight { get; set; }
    }
}