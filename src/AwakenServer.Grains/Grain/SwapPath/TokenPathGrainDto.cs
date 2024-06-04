namespace AwakenServer.Grains.Grain.SwapTokenPath;

using AwakenServer.Trade.Dtos;

public class TokenPath
{
    public double FeeRate { get; set; }
    public List<PathNode> Path { get; set; } = new List<PathNode>();
}

public class PathNode
{
    public string Token0Symbol { get; set; }
    public string Token1Symbol { get; set; }
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
    public List<TradePairDto> Relations { get; set; }
}