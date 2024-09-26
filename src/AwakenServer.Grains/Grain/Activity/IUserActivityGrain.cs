using Orleans;

namespace AwakenServer.Grains.Grain.Activity;

public interface IUserActivityGrain : IGrainWithStringKey
{
    Task<GrainResultDto<UserActivityGrainDto>> AddUserPointAsync(string userAddress, double point, long timestamp);
}