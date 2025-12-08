namespace AwakenServer.Grains.State.MyPortfolio;

[GenerateSerializer]
public class UserLiquiditySnapshotState
{
    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public Guid TradePairId { get; set; }
    [Id(2)]
    public string Address { get; set; }
    [Id(3)]
    public long LpTokenAmount { get; set; }
    [Id(4)]
    public DateTime SnapShotTime { get; set; }
    [Id(5)]
    public long Token0TotalFee { get; set; }
    [Id(6)]
    public long Token1TotalFee { get; set; }
    [Id(7)]
    public bool IsDeleted { get; set; }
}