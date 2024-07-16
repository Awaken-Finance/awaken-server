using System.Collections.Generic;

namespace AwakenServer.Trade.Dtos;

public class PercentRouteDto
{
    public string Percent { get; set; }
    public List<SwapRecord> Route { get; set; }
}