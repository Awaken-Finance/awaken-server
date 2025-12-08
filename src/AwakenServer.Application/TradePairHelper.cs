using System;

namespace AwakenServer;

public class TradePairHelper
{
    public static string GetLpToken(string token0, string token1)
    {
        var tokens = new string[] { token0, token1 };
        Array.Sort(tokens);
        return $"ALP {tokens[0]}-{tokens[1]}";
    }
}