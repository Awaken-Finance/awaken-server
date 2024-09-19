using System;
using AwakenServer.Entities;
using AwakenServer.Trade.Index;
using Nest;

namespace AwakenServer.StatInfo;

public class PoolStatInfo : MultiChainEntity<Guid>
{
    [Keyword] public string Version { get; set; }
    [Keyword] public string PairAddress { get; set; }
    public TradePairWithToken TradePair { get; set; }
    public double Tvl { get; set; }
    public double ValueLocked0 { get; set; }
    public double ValueLocked1 { get; set; }
    public double VolumeInUsd24h { get; set; }
    public double VolumeInUsd7d { get; set; }
    public long TransactionCount { get; set; }
    public double Price { get; set; }
    public long LastUpdateTime { get; set; }
}