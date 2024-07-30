using System.Collections.Generic;
using AwakenServer.Tokens;
using AwakenServer.Trade.Dtos;

namespace AwakenServer.Route.Dtos;

public class BestRoutesDto
{
    public List<RouteDto> Routes { get; set; } = new();
}

public class RouteDto
{
    public string AmountIn { get; set; }
    public string AmountOut { get; set; }
    public long Splits { get; set; }
    public List<PercentRouteDto> Distributions { get; set; }
}

public class TradePairExtensionDto
{
    public string ValueLocked0 { get; set; }
    public string ValueLocked1 { get; set; }
}

public class PercentRouteDto
{
    public int Percent { get; set; }
    public string AmountIn { get; set; }
    public string AmountOut { get; set; }
    public List<TradePairWithTokenDto> TradePairs { get; set; }
    public List<TradePairExtensionDto> TradePairExtensions { get; set; }
    public List<TokenDto> Tokens { get; set; }
    public List<string> Amounts { get; set; }
    public List<double> FeeRates { get; set; }
}