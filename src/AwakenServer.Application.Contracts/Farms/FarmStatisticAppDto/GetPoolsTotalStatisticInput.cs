using System;

namespace AwakenServer.Farms
{
    public class GetPoolsTotalStatisticInput
    {
        public string? ChainId { get; set; }
        public Guid? FarmId { get; set; }

        public Guid? PoolId { get; set; }
    }
}