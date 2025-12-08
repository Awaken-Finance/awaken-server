using System.Collections.Generic;

namespace AwakenServer.Trade
{
    public class StableCoinOptions
    {
        public Dictionary<string, List<Coin>> Coins { get; set; } = new();
    }
}