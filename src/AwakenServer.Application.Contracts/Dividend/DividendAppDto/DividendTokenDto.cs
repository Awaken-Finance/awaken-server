using AwakenServer.Tokens;

namespace AwakenServer.Dividend.DividendAppDto
{
    public class DividendTokenDto
    {
        public TokenDto Token { get; set; }
        public long StartBlock { get; set; }
        public long EndBlock { get; set; }
        public string AmountPerBlock { get; set; }
    }
}