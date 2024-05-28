using AwakenServer.Trade;

namespace AwakenServer.Grains.Grain.Trade;

public class LiquidityRecordGrainDto
{
    public string TransactionHash { get; set; }
    public string ChainId { get; set; }
    public string Pair { get; set; }
    public string LpTokenAmount { get; set; }
    public DateTime Timestamp { get; set; }
    public LiquidityType Type { get; set; }
    public long BlockHeight { get; set; }
    public bool IsRevert { get; set; }
    
    public string TotalSupply { get; set; }
}