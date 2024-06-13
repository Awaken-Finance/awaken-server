namespace AwakenServer.Grains.State.MyPortfolio;

public class UserLiquiditySnapshotState
{
    public Guid TradePairId { get; set; }
    public string Address { get; set; }
    public long LpTokenAmount { get; set; }
    public long SnapShotTime { get; set; }
    public long Token0TotalFee { get; set; }
    public long Token1TotalFee { get; set; }
    public bool IsDeleted { get; set; }
}