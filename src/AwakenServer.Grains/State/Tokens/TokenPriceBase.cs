namespace AwakenServer.Grains.State.Tokens;

[GenerateSerializer]
public class TokenPriceBase
{
    [Id(0)]
    public string Id { get; set; }
    [Id(1)]
    public string Symbol { get; set; }
    [Id(2)]
    public decimal PriceInUsd { get; set; }
}