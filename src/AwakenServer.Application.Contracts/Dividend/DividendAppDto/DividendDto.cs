using System;
using System.Collections.Generic;

namespace AwakenServer.Dividend.DividendAppDto
{
    public class DividendBaseDto
    {
        public Guid Id { get; set; }
        public string ChainId { get; set; }
        public string Address { get; set; }
    }

    public class DividendDto : DividendBaseDto
    {
        public int TotalWeight { get; set; }
        public List<DividendTokenDto> DividendTokens { get; set; }
    }
}