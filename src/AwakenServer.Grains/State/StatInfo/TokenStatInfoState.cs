namespace AwakenServer.Grains.State.StatInfo;

public class TokenStatInfoState
{
    public string Symbol { get; set; }
    public string FollowPairAddress { get; set; }
    public double ValueLocked { get; set; }
    public double Tvl { get; set; }
    public double VolumeInUsd24h { get; set; }
    public double Price { get; set; }
    public double PricePercentChange24h { get; set; }
    public long TransactionCount { get; set; }
    public long PoolCount { get; set; }
    public DateTime LastUpdateTime { get; set; }
}