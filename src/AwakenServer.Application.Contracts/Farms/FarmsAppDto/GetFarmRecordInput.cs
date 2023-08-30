using System;
using System.ComponentModel.DataAnnotations;

namespace AwakenServer.Farms
{
    public class GetFarmRecordInput
    {
        public string? ChainId { get; set; }
        public Guid? FarmId { get; set; }
        [Required]
        public string User { get; set; }
        public Guid? TokenId { get; set; }
        public long StartTime { get; set; }
        public long EndTime { get; set; }
        public BehaviorType? BehaviorType { get; set; }
        public int SkipCount { get; set; }
        public int Size { get; set; }
        public bool IsAscend { get; set; }
    }
}