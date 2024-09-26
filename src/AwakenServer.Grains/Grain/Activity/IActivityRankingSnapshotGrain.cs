using Orleans;

namespace AwakenServer.Grains.Grain.Activity;

public interface IActivityRankingSnapshotGrain : IGrainWithStringKey
{
    Task<GrainResultDto<ActivityRankingSnapshotGrainDto>> AddOrUpdateAsync(ActivityRankingSnapshotGrainDto dto);
}