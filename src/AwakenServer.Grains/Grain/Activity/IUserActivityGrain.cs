using Orleans;

namespace AwakenServer.Grains.Grain.Activity;

public interface IUserActivityGrain : IGrainWithStringKey
{
    Task<GrainResultDto<UserActivityGrainDto>> AccumulateUserPointAsync(int activityId, string userAddress,
        double point, long timestamp);
    Task<GrainResultDto<UserActivityGrainDto>> GetAsync();
}