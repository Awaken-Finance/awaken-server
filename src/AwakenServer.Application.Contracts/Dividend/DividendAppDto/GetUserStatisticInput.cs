using System;
using System.ComponentModel.DataAnnotations;

namespace AwakenServer.Dividend.DividendAppDto
{
    public class GetUserStatisticInput
    {
        [Required]
        public Guid DividendId { get; set; }
        [Required]
        public string User { get; set; }
    }
}