using System;

namespace AwakenServer.Dividend.DividendAppDto
{
    public class GetDividendInput
    {
        public string? ChainId { get; set; }
        public Guid? DividendId { get; set; }
    }
}