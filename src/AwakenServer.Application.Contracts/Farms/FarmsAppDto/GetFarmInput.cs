using System;

namespace AwakenServer.Farms
{
    public class GetFarmInput
    {
        public string? ChainId { get; set; }
        public Guid? FarmId { get; set; }
    }
}