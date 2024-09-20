namespace AwakenServer.Grains.State.StatInfo;

public class StatInfoSnapshotState
{
    public string Version { get; set; }
    public Guid Id { get; set; }
    public string ChainId { get; set; }
    public int StatType { get; set; } // 0 all 1 token 2 pool
    public string Symbol { get; set; }// for StatType= 1
    public string PairAddress { get; set; } // for StatType= 2
    public double Tvl { get; set; }
    public double VolumeInUsd { get; set; }
    public double Price { get; set; }
    public double PriceInUsd { get; set; }
    public double LpFeeInUsd { get; set; }// for StatType= 2
    public long Period { get; set; }
    public long Timestamp { get; set; }
}