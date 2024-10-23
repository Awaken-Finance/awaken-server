using AwakenServer.Trade;

namespace AwakenServer.Grains.Grain.MyPortfolio;

[GenerateSerializer]
public class CurrentUserLiquidityGrainDto
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
    public DateTime LastUpdateTime { get; set; }
    [Id(7)]
    public long Token0CumulativeAddition { get; set; }
    [Id(8)]
    public long Token1CumulativeAddition { get; set; }
    [Id(9)]
    public DateTime AverageHoldingStartTime { get; set; }
    [Id(10)]
    public long Token0UnReceivedFee { get; set; }
    [Id(11)]
    public long Token1UnReceivedFee { get; set; }
    [Id(12)]
    public long Token0ReceivedFee { get; set; }
    [Id(13)]
    public long Token1ReceivedFee { get; set; }
    [Id(14)]
    public double AssetInUSD { get; set; }
}