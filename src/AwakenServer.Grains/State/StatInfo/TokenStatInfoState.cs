namespace AwakenServer.Grains.State.StatInfo;

public class TokenStatInfoState
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public string FollowPairAddress { get; set; }
    public double ValueLocked { get; set; }
    public double Tvl { get; set; }
    public double VolumeInUsd24h { get; set; }
    public double PriceInUsd { get; set; }
    public double PricePercentChange24h { get; set; }
    public long TransactionCount { get; set; }
    public long PoolCount { get; set; }
    public long LastUpdateTime { get; set; }
}