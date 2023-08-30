using System;
using System.ComponentModel.DataAnnotations;

namespace AwakenServer.Dividend.DividendAppDto
{
    public class GetDividendPoolStatisticInput
    {
        [Required]
        public Guid DividendId { get; set; }
    }
}