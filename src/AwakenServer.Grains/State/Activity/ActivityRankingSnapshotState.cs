namespace AwakenServer.Grains.State.Activity;

public class ActivityRankingSnapshotState
{
    public Guid Id { get; set; }
    public int ActivityId { get; set; }
    public long Timestamp { get; set; }
    public long NumOfJoin { get; set; } // JoinRecord Count union UserActivityInfo count
    public List<RankingInfo> RankingList { get; set; } = new();
}


public class RankingInfo
{
    public string Address { get; set; }
    public double TotalPoint { get; set; }
}