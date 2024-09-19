using System;
using AwakenServer.Entities;
using Nest;

namespace AwakenServer.StatInfo;

public class TokenStatInfo : MultiChainEntity<Guid>
{
    [Keyword] public string Version { get; set; }
    [Keyword] public string Symbol { get; set; }
    [Keyword] public string FollowPairAddress { get; set; }
    public double ValueLocked { get; set; }
    public double Tvl { get; set; }
    public double VolumeInUsd24h { get; set; }
    public double PriceInUsd { get; set; }
    public double PricePercentChange24h { get; set; }
    public long TransactionCount { get; set; }
    public long PoolCount { get; set; }
    public long LastUpdateTime { get; set; }
}