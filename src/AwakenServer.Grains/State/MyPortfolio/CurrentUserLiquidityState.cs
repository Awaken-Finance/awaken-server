namespace AwakenServer.Grains.State.MyPortfolio;

public class CurrentUserLiquidityState
{
    public Guid TradePairId { get; set; }
    public string Address { get; set; }
    public long LpTokenAmount { get; set; }
    public long LastUpdateTime { get; set; }
    public long Token0CumulativeAddition { get; set; }
    public long Token1CumulativeAddition { get; set; }
    public long AverageHoldingStartTime { get; set; }
    public long Token0UnReceivedFee { get; set; }
    public long Token1UnReceivedFee { get; set; }
    public long Token0ReceivedFee { get; set; }
    public long Token1ReceivedFee { get; set; }
    public bool IsDeleted { get; set; }
}