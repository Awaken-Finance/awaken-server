namespace AwakenServer.Grains.State.Activity;

[GenerateSerializer]
public class CurrentActivityRankingState
{
    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public int ActivityId { get; set; }
    [Id(2)]
    public long Timestamp { get; set; }
    [Id(3)]
    public long NumOfJoin { get; set; } // JoinRecord Count union UserActivityInfo count
    [Id(4)]
    public List<RankingInfo> RankingList { get; set; } = new();
}