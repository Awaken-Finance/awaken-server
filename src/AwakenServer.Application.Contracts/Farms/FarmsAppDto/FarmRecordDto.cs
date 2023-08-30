using System;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Farms
{
    public class FarmRecordDto: EntityDto<Guid>
    {
        public string TransactionHash { get; set; }
        public string User { get; set; }
        public string Amount { get; set; }
        public double DecimalAmount { get; set; }
        public long Timestamp { get; set; }
        public BehaviorType BehaviorType { get; set; }
        public FarmBaseDto FarmInfo { get; set; }
        public FarmPoolBaseDto PoolInfo { get; set; }
        public FarmTokenDto TokenInfo { get; set; }
    }
}