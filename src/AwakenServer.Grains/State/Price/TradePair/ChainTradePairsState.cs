using System.Collections.Generic;

namespace AwakenServer.Grains.State.Price;

[GenerateSerializer]
public class ChainTradePairsState
{
    [Id(0)]
    public Dictionary<string, string> TradePairs { get; set; } = new Dictionary<string, string>();
}