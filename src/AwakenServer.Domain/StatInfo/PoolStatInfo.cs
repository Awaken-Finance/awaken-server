using System;
using AwakenServer.Entities;
using AwakenServer.Trade.Index;
using Nest;

namespace AwakenServer.StatInfo;

public class PoolStatInfo : MultiChainEntity<Guid>
{
    [Keyword] public string Version { get; set; }
    public TradePairWithToken TradePair { get; set; }
    public double Tvl { get; set; }
    public long ReserveA { get; set; }
    public long ReserveB { get; set; }
    public double VolumeInUsd24h { get; set; }
    public double VolumeInUsd7d { get; set; }
    public double Price { get; set; }
    public long TransactionCount { get; set; }
    public DateTime LastUpdateTime { get; set; }
}