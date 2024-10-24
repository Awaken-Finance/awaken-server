using AutoMapper;
using AwakenServer.StatInfo;

namespace AwakenServer.Grains.Grain.StatInfo;

[AutoMap(typeof(StatInfoSnapshot))]
[GenerateSerializer]
public class StatInfoSnapshotGrainDto
{
    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public string ChainId { get; set; }
    [Id(2)]
    public string Version { get; set; }
    [Id(3)]
    public int StatType { get; set; } // 0 all 1 token 2 pool
    [Id(4)]
    public string Symbol { get; set; }// for StatType= 1
    [Id(5)]
    public string PairAddress { get; set; } // for StatType= 2
    [Id(6)]
    public double Tvl { get; set; }
    [Id(7)]
    public double VolumeInUsd { get; set; }
    [Id(8)]
    public double Price { get; set; } // for StatType = 2
    [Id(9)]
    public double PriceInUsd { get; set; } // for StatType = 1
    [Id(10)]
    public double LpFeeInUsd { get; set; }// for StatType= 2
    [Id(11)]
    public long Period { get; set; }
    [Id(12)]
    public long Timestamp { get; set; }
}