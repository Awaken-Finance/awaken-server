using System;
using AwakenServer.Trade.Index;

namespace AwakenServer.StatInfo;

public class PoolStatInfo
{
    public TradePairWithToken TradePair;
    public double Tvl;
    public double TotalVolumeInUsd;
    public double Price;
    public double TotalLpFeeInUsd;
    public DateTime LastUpdateTime;
}