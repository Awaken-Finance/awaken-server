using System.Collections.Generic;
using AwakenServer.Tokens;

namespace AwakenServer.Dividend.DividendAppDto
{
    public class DividendUserPoolDto
    {
        public string DepositAmount { get; set; }
        public DividendPoolBaseInfoDto PoolBaseInfo { get; set; }
    }

    public class DividendUserTokenDto
    {
        public TokenDto DividendToken { get; set; }
        public string AccumulativeDividend { get; set; }
    }

    public class DividendUserInformationDto
    {
        public string User { get; set; }
        public List<DividendUserPoolDto> UserPools { get; set; }
        public List<DividendUserTokenDto> UserTokens { get; set; }
    }
}