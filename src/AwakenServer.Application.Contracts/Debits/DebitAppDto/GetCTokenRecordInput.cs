using System;
using System.ComponentModel.DataAnnotations;

namespace AwakenServer.Debits.DebitAppDto
{
    public class GetCTokenRecordInput
    {
        public string? ChainId { get; set; }
        public Guid? CompControllerId { get; set; }
        [Required]
        public string User { get; set; }
        public Guid? CTokenId { get; set; }
        public long StartTime { get; set; }
        public long EndTime { get; set; }
        public BehaviorType? BehaviorType { get; set; }
        public int SkipCount { get; set; }
        public int Size { get; set; }
        public bool IsAscend { get; set; }
    }
}