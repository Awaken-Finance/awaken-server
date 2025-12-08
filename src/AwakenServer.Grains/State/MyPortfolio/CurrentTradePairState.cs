namespace AwakenServer.Grains.State.MyPortfolio;

[GenerateSerializer]
public class CurrentTradePairState
{
    [Id(0)] public Guid TradePairId { get; set; }
    [Id(1)] public long TotalSupply { get; set; }
    [Id(2)] public DateTime LastUpdateTime { get; set; }
    [Id(3)] public long Token0TotalFee { get; set; }
    [Id(4)] public long Token1TotalFee { get; set; }
    [Id(5)] public bool IsDeleted { get; set; }
}