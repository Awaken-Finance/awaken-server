using System;

namespace AwakenServer.Farms
{
    public class GetFarmPoolInput
    {
        public string? ChainId { get; set; }
        public Guid? FarmId { get; set; }
        public Guid? PoolId { get; set; }
        
        public string User { get; set; }
        public bool IsUpdateReward { get; set; }
    }
}