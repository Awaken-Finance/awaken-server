using System;

namespace AwakenServer.StatInfo;

public class TokenStatInfo
{
    public string Symbol;
    public string FollowPairAddress;
    public double Tvl;
    public double Volume24h;
    public double Price;
    public double PricePercentChange24h;
    public DateTime LastUpdateTime;
}