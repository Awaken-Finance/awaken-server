using System.Collections.Generic;

namespace AwakenServer
{
    public class MainCoinOptions
    {
        public Dictionary<string, Dictionary<string, Coin>> Coins { get; set; } = new();

        public Coin GetCoin(string symbol, string chainName)
        {
            if (Coins.TryGetValue(symbol, out var coins)&& coins.TryGetValue(chainName,out var coin))
            {
                return coin;
            }

            return null;
        }
    }
}