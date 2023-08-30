using System;
using System.ComponentModel.DataAnnotations;

namespace AwakenServer.Farms
{
    public class GetUsersTotalStatisticInput
    {
        [Required]
        public string User { get; set; }
        [Required]
        public string? ChainId { get; set; }
        public Guid? FarmId { get; set; }
        public Guid? PoolId { get; set; }
    }
}