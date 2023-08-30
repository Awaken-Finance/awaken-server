using AwakenServer.Tokens;

namespace AwakenServer.Dividend.DividendAppDto
{
    public class DividendPoolTokenDto
    {
        public TokenDto DividendToken { get; set; }
        public string AccumulativeDividend { get; set; }
        public string ToDistributedDivided { get; set; }
        public long LastRewardBlock { get; set; }
    }
}