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
    public long AmountIn { get; set; }
    public long AmountOut { get; set; }
    public long Splits { get; set; }
    public List<PercentRouteDto> Distributions { get; set; }
}

public class PercentRouteDto
{
    public int Percent { get; set; }
    public long AmountIn { get; set; }
    public long AmountOut { get; set; }
    public List<TradePairWithTokenDto> TradePairs { get; set; }
    public List<TokenDto> Tokens { get; set; }
    public List<long> Amounts { get; set; }
    public List<double> FeeRates { get; set; }
}