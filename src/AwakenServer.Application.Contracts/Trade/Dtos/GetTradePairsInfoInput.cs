namespace AwakenServer.Trade.Dtos;

public class GetTradePairsInfoInput
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public string Token0Symbol { get; set; }
    public string Token1Symbol { get; set; }
    public double FeeRate { get; set; }
    public string Address { get; set; }
    public string TokenSymbol { get; set; }
    public long StartBlockHeight { get; set; }
    public long EndBlockHeight { get; set; }
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; }
}