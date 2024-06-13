namespace AwakenServer.Grains.State.MyPortfolio;

public class CurrentTradePairState
{
    public Guid TradePairId { get; set; }
    public long TotalSupply { get; set; }
    public DateTime LastUpdateTime { get; set; }
    public long Token0TotalFee { get; set; }
    public long Token1TotalFee { get; set; }
    public bool IsDeleted { get; set; }
}