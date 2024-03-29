using System.Collections.Generic;

namespace AwakenServer.ContractEventHandler.Trade
{
    public class TradePairTokenOrderOptions
    {
        public List<TradePairToken> TradePairTokens { get; set; } = new();
    }

    public class TradePairToken
    {
        public string Address { get; set; }
        public string Symbol { get; set; }
        public int Weight { get; set; }
    }
}