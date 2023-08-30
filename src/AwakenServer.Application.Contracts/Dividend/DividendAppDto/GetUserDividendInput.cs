using System;
using System.ComponentModel.DataAnnotations;

namespace AwakenServer.Dividend.DividendAppDto
{
    public class GetUserDividendInput
    {
        [Required]
        public string User { get; set; }
        [Required]
        public Guid DividendId { get; set; }
    }
}