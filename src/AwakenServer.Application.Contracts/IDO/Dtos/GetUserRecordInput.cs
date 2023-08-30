using System;
using System.ComponentModel.DataAnnotations;

namespace AwakenServer.IDO.Dtos
{
    public class GetUserRecordInput : PageInputBase
    {
        [Required]
        public string ChainId { get; set; }
        [Required]
        public string User { get; set; }
        public long TimestampMin { get; set; }
        public long TimestampMax { get; set; }
        public Guid? TokenId { get; set; }
        public Guid? RaiseTokenId { get; set; }
        public int OperateType { get; set; }
    }
}