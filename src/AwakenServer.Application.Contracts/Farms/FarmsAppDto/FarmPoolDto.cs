using System;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Farms
{
    public class FarmPoolVariableInfo : EntityDto<Guid>
    {
        public int Weight { get; set; }
        public long LastUpdateBlockHeight { get; set; }
        public string TotalDepositAmount { get; set; }
        public string AccumulativeDividendProjectToken { get; set; }
        public string AccumulativeDividendUsdt { get; set; }
        public string PendingProjectToken { get; set; }
        public string PendingUsdt { get; set; }
        public decimal Apy1 { get; set; }
        public decimal Apy2 { get; set; }
    }

    public class FarmPoolDto : FarmPoolVariableInfo
    {
        public string ChainId { get; set; }
        public Guid FarmId { get; set; }
        public int Pid { get; set; }
        public PoolType PoolType { get; set; }
        public FarmTokenDto SwapToken { get; set; }
        public FarmTokenDto Token1 { get; set; }
        public FarmTokenDto Token2 { get; set; }
    }
}