using AwakenServer.Tokens;
using AwakenServer.Trade.Index;

namespace AwakenServer.Grains.Grain.SwapTokenPath;

using AwakenServer.Trade.Dtos;

[GenerateSerializer]
public class TokenPath
{
    [Id(0)]
    public double FeeRate { get; set; }
    [Id(1)]
    public List<PathNode> Path { get; set; } = new ();
    [Id(2)]
    public string FullPathStr { get; set; }
    [Id(3)]
    public List<TokenDto> RawPath { get; set; } = new ();
}

[GenerateSerializer]
public class PathNode
{
    [Id(0)]
    public TokenDto Token0 { get; set; }
    [Id(1)]
    public TokenDto Token1 { get; set; }
    [Id(2)]
    public string Address { get; set; }
    [Id(3)]
    public double FeeRate { get; set; }
}

[GenerateSerializer]
public class TokenPathResultGrainDto
{
    [Id(0)]
    public List<TokenPath> Path { get; set; }
}

[GenerateSerializer]
public class GetTokenPathGrainDto
{
    [Id(0)]
    public string ChainId { get; set; }
    [Id(1)]
    public string StartSymbol { get; set; }
    [Id(2)]
    public string EndSymbol { get; set; }
    [Id(3)]
    public int MaxDepth { get; set; } = 3;
}

[GenerateSerializer]
public class GraphDto
{
    [Id(0)]
    public List<TradePairWithTokenDto> Relations { get; set; }
}