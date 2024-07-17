using AwakenServer.Tokens;
using AwakenServer.Trade.Index;

namespace AwakenServer.Grains.Grain.SwapTokenPath;

using AwakenServer.Trade.Dtos;

public class TokenPath
{
    public double FeeRate { get; set; }
    public List<PathNode> Path { get; set; } = new ();
    public string FullPathStr { get; set; }
    public List<TokenDto> RawPath { get; set; } = new ();
}

public class PathNode
{
    public TokenDto Token0 { get; set; }
    public TokenDto Token1 { get; set; }
    public string Address { get; set; }
    public double FeeRate { get; set; }
}

public class TokenPathResultGrainDto
{
    public List<TokenPath> Path { get; set; }
}


public class GetTokenPathGrainDto
{
    public string ChainId { get; set; }
    public string StartSymbol { get; set; }
    public string EndSymbol { get; set; }
    public int MaxDepth { get; set; } = 3;
}

public class GraphDto
{
    public List<TradePairWithToken> Relations { get; set; }
}