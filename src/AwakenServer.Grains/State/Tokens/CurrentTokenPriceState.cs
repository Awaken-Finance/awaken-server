namespace AwakenServer.Grains.State.Tokens;

public class CurrentTokenPriceState : TokenPriceBase
{
    public DateTime PriceUpdateTime { get; set; }
}