namespace AwakenServer.Grains.Grain.Price;

[GenerateSerializer]
public class ChainTradePairsGrainDto
{
    [Id(0)]
    public string TradePairAddress { get; set; }

    [Id(1)]
    public string TradePairGrainId { get; set; }
}