using AwakenServer.Tokens;
using AwakenServer.Trade.Index;

namespace AwakenServer.Grains.Grain.Route;

using AwakenServer.Trade.Dtos;

public class SwapRoute
{
    public List<TradePairWithToken> TradePairs { get; set; } = new();
    public List<Token> Tokens { get; set; } = new();
    public List<double> FeeRates { get; set; } = new();
    public string FullPathStr { get; set; }
}

public class RoutesResultGrainDto
{
    public List<SwapRoute> Routes { get; set; } = new();
}

public class SearchRoutesGrainDto
{
    public string ChainId { get; set; }
    public string SymbolBegin { get; set; }
    public string SymbolEnd { get; set; }
    public int MaxDepth { get; set; } = 3;
    public List<TradePairWithToken> Relations { get; set; }
}

public class GetRoutesGrainDto
{
    public string ChainId { get; set; }
    public string SymbolBegin { get; set; }
    public string SymbolEnd { get; set; }
    public int MaxDepth { get; set; } = 3;
}