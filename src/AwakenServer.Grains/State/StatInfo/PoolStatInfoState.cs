using AwakenServer.Trade.Index;

namespace AwakenServer.Grains.State.StatInfo;

public class PoolStatInfoState
{
    public string ChainId { get; set; }
    public string PairAddress { get; set; }
    public double Tvl { get; set; }
    public double Price { get; set; }
    public double ValueLocked0 { get; set; }
    public double ValueLocked1 { get; set; }
    public double VolumeInUsd24h { get; set; }
    public double VolumeInUsd7d { get; set; }
    public long TransactionCount { get; set; }
    public long LastUpdateTime { get; set; }
}