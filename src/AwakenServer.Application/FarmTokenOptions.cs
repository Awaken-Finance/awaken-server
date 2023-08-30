using System.Collections.Generic;

namespace AwakenServer
{
    public class FarmTokenOptions
    {
        public Dictionary<string, FarmToken> FarmTokens { get; set; }
    }

    public enum FarmTokenType
    {
        LpToken,
        GToken,
        OtherLpToken,
        AToken
    }

    public class FarmToken
    {
        public string ChainName { get; set; }
        public string Symbol { get; set; }
        public int Decimals { get; set; }
        public string Address { get; set; }
        public TokenOption[] Tokens { get; set; }
        public FarmTokenType Type { get; set; }
        public string LendingPool { get; set; }
        public string TradePairAddress { get; set; }
    }

    public class TokenOption
    {
        public string Address { get; set; }

        public string Symbol { get; set; }

        public int Decimals { get; set; }
    }
}