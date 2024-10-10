using AwakenServer.Trade;

namespace AwakenServer.Grains.Grain.MyPortfolio;

[GenerateSerializer]
public class UserLiquiditySnapshotGrainDto/* : UserLiquiditySnapshot*/
{
    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public string ChainId { get; set; }
    [Id(2)]
    public Guid TradePairId { get; set; }
    [Id(3)]
    public string Address { get; set; }
    [Id(4)]
    public string Version { get; set; }
    [Id(5)]
    public long LpTokenAmount { get; set; }
    [Id(6)]
    public DateTime SnapShotTime { get; set; }
    [Id(7)]
    public long Token0TotalFee { get; set; }
    [Id(8)]
    public long Token1TotalFee { get; set; }
}