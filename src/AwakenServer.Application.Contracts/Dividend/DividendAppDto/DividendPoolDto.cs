using System;
using System.Collections.Generic;
using AwakenServer.Tokens;

namespace AwakenServer.Dividend.DividendAppDto
{
    public class DividendPoolBaseInfoDto
    {
        public Guid Id { get; set; }
        public int Pid { get; set; }
        public TokenDto PoolToken { get; set; }
        public DividendBaseDto Dividend { get; set; }
    }

    public class DividendPoolDto : DividendPoolBaseInfoDto
    {
        public double Apy { get; set; }
        public int Weight { get; set; }
        public string DepositAmount { get; set; }
        public List<DividendPoolTokenDto> DividendTokenInfo { get; set; }
    }
}