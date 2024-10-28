using AwakenServer.StatInfo;
using Nest;

namespace AwakenServer.Grains.Grain.StatInfo;

[GenerateSerializer]
public class TokenStatInfoGrainDto 
{
    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public string ChainId { get; set; }
    [Id(2)]
    public string Version { get; set; }
    [Id(3)]
    public string Symbol { get; set; }
    [Id(4)]
    public string FollowPairAddress { get; set; }
    [Id(5)]
    public double ValueLocked { get; set; }
    [Id(6)]
    public double Tvl { get; set; }
    [Id(7)]
    public double VolumeInUsd24h { get; set; }
    [Id(8)]
    public double PriceInUsd { get; set; }
    [Id(9)]
    public double PricePercentChange24h { get; set; }
    [Id(10)]
    public long TransactionCount { get; set; }
    [Id(11)]
    public long PoolCount { get; set; }
    [Id(12)]
    public long LastUpdateTime { get; set; }
}