using System.Collections.Generic;

namespace AwakenServer.Trade.Dtos;

public class SwapDetailDto
{
    public string PairAddress { get; set; }
    public TradePairWithTokenDto TradePair { get; set; }
    public long AmountOut { get; set; }
    public long AmountIn { get; set; }
    public long TotalFee { get; set; }
    public string SymbolOut { get; set; }
    public string SymbolIn { get; set; }
    public string Channel { get; set; }
}

public class PercentRouteDto
{
    public string Percent { get; set; }
    public List<SwapDetailDto> Route { get; set; }
}