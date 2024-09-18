using System;
using AwakenServer.Trade.Index;

namespace AwakenServer.StatInfo;

public class PoolStatInfo
{
    public TradePairWithToken TradePair;
    public double Tvl;
    public double ValueLocked0;
    public double ValueLocked1;
    public double VolumeInUsd24h;
    public double VolumeInUsd7d;
    public double Price;
    public DateTime LastUpdateTime;
}