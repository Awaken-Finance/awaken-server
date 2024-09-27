using Orleans;

namespace AwakenServer.Grains.Grain.Activity;

public interface IUserActivityGrain : IGrainWithStringKey
{
    Task<GrainResultDto<UserActivityGrainDto>> AccumulateUserPointAsync(string userAddress, double point, long timestamp);
    Task<GrainResultDto<UserActivityGrainDto>> UpdateUserPointAsync(string userAddress, double point, long timestamp);
    Task<GrainResultDto<UserActivityGrainDto>> GetAsync();
}