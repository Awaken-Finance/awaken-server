using AwakenServer.Grains.State.Activity;

namespace AwakenServer.Grains.Grain.Activity;

[GenerateSerializer]
public class ActivityRankingSnapshotGrainDto
{
    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public string ChainId { get; set; }
    [Id(2)]
    public int ActivityId { get; set; }
    [Id(3)]
    public long Timestamp { get; set; }
    [Id(4)]
    public long NumOfJoin { get; set; } // JoinRecord Count union UserActivityInfo count
    [Id(5)]
    public List<RankingInfo> RankingList { get; set; }
}
