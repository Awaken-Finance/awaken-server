using System;

namespace AwakenServer.Farms
{
    public class GetFarmUserInfoInput
    {
        public string? ChainId { get; set; }
        public Guid? FarmId { get; set; }
        public Guid? PoolId { get; set; }
        public bool IsWithDetailPool { get; set; }
        public bool IsWithApy { get; set; }
        public string User { get; set; }
    }
}