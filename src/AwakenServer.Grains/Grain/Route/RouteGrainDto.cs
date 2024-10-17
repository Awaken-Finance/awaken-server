using AwakenServer.Tokens;
using AwakenServer.Trade.Index;

namespace AwakenServer.Grains.Grain.Route;

using AwakenServer.Trade.Dtos;

[GenerateSerializer]
public class SwapRoute
{
    [Id(0)]
    public List<TradePairWithToken> TradePairs { get; set; } = new();
    [Id(1)]
    public List<Token> Tokens { get; set; } = new();
    [Id(2)]
    public List<double> FeeRates { get; set; } = new();
    [Id(3)]
    public string FullPathStr { get; set; }
}

[GenerateSerializer]
public class RoutesResultGrainDto
{
    [Id(0)]
    public List<SwapRoute> Routes { get; set; } = new();
}

[GenerateSerializer]
public class SearchRoutesGrainDto
{
    [Id(0)]
    public string ChainId { get; set; }
    [Id(1)]
    public string SymbolBegin { get; set; }
    [Id(2)]
    public string SymbolEnd { get; set; }
    [Id(3)]
    public int MaxDepth { get; set; } = 3;
    [Id(4)]
    public List<TradePairWithTokenDto> Relations { get; set; }
}

[GenerateSerializer]
public class GetRoutesGrainDto
{
    [Id(0)]
    public string ChainId { get; set; }
    [Id(1)]
    public string SymbolBegin { get; set; }
    [Id(2)]
    public string SymbolEnd { get; set; }
    [Id(3)]
    public int MaxDepth { get; set; } = 3;
}