using System;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Farms
{
    public class FarmUserInfoDto: EntityDto<Guid>
    {
        public string ChainId { get; set; }
        public string User { get; set; }
        public string CurrentDepositAmount { get; set; }
        public string AccumulativeDividendProjectTokenAmount { get; set; }
        public string AccumulativeDividendUsdtAmount { get; set; }
        public FarmBaseDto FarmInfo { get; set; }
        public FarmPoolBaseDto PoolInfo { get; set; }
        public FarmTokenDto SwapToken { get; set; }
        public FarmTokenDto Token1 { get; set; }
        public FarmTokenDto Token2 { get; set; }
        public FarmPoolVariableInfo PoolDetailInfo { get; set; }
    }
}