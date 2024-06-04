namespace AwakenServer.SwapTokenPath.Dtos;

public class GetTokenPathsInput
{
    public string ChainId { get; set; }
    public string StartSymbol { get; set; }
    public string EndSymbol { get; set; }
    public int MaxDepth { get; set; } = 3;
}