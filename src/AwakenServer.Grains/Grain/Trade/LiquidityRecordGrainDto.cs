using AwakenServer.Trade;

namespace AwakenServer.Grains.Grain.Trade;

[GenerateSerializer]
public class LiquidityRecordGrainDto
{
    [Id(0)]
    public string TransactionHash { get; set; }
    [Id(1)]
    public string ChainId { get; set; }
    [Id(2)]
    public string Pair { get; set; }
    [Id(3)]
    public string LpTokenAmount { get; set; }
    [Id(4)]
    public DateTime Timestamp { get; set; }
    [Id(5)]
    public LiquidityType Type { get; set; }
    [Id(6)]
    public long BlockHeight { get; set; }
    [Id(7)]
    public bool IsRevert { get; set; }

    [Id(8)]
    public string TotalSupply { get; set; }
}