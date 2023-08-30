using System;
using System.ComponentModel.DataAnnotations;

namespace AwakenServer.Dividend.DividendAppDto
{
    public class GetDividendUserRecordsInput
    {
        [Required]
        public Guid DividendId { get; set; }
        [Required]
        public string User { get; set; }
        public long TimestampMin { get; set; }
        public long TimestampMax { get; set; }
        public Guid? PoolId { get; set; }
        public Guid? TokenId { get; set; }
        public BehaviorType BehaviorType { get; set; }
        public int SkipCount { get; set; }
        public int Size { get; set; }
    }
}