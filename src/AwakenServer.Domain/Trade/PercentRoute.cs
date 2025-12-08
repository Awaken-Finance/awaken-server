using System.Collections.Generic;

namespace AwakenServer.Trade;

public class PercentRoute
{
    public string Percent { get; set; }
    public List<SwapRecord> Route { get; set; }
}