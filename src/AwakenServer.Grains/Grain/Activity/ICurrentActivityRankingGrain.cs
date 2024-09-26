using Orleans;

namespace AwakenServer.Grains.Grain.Activity;

public interface ICurrentActivityRankingGrain : IGrainWithStringKey
{
    Task<GrainResultDto<ActivityRankingSnapshotGrainDto>> AddOrUpdateAsync(string userAddress, double totalPoint,
        long timestamp, int activityId);
}