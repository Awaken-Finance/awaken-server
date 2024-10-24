using AwakenServer.Trade.Dtos;

namespace AwakenServer.StatInfo.Dtos;

public class PoolStatInfoDto
{
    public TradePairWithTokenDto TradePair { get; set; }
    public double Tvl { get; set; }
    public long TransactionCount { get; set; }
    public double Volume24hInUsd { get; set; }
    public double Volume7dInUsd { get; set; }
    public double Apr7d { get; set; }
    public double ValueLocked0 { get; set; }
    public double ValueLocked1 { get; set; }
}