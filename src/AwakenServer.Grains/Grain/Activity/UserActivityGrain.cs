using AwakenServer.Grains.State.Activity;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Grains.Grain.Activity;

public class UserActivityGrain : Grain<UserActivityState>, IUserActivityGrain
{
    private readonly IObjectMapper _objectMapper;
    public UserActivityGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    
    public async Task<GrainResultDto<UserActivityGrainDto>> AddUserPointAsync(string userAddress, double point, long timestamp)
    {
        if (string.IsNullOrEmpty(State.Address))
        {
            State.Address = userAddress;
        }
        State.TotalPoint += point;
        State.LastUpdateTime = timestamp;
        await WriteStateAsync();
        return new GrainResultDto<UserActivityGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<UserActivityState, UserActivityGrainDto>(State)
        };
    }

}