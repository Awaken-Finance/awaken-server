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

    
    public async Task<GrainResultDto<UserActivityGrainDto>> AccumulateUserPointAsync(int activityId, string userAddress,
        double point, long timestamp)
    {
        if (string.IsNullOrEmpty(State.Address))
        {
            State.Address = userAddress;
            State.ActivityId = activityId;
            State.Id = Guid.NewGuid();
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

    public async Task<GrainResultDto<UserActivityGrainDto>> GetAsync()
    {
        await ReadStateAsync();
        if (string.IsNullOrEmpty(State.Address))
        {
            return new GrainResultDto<UserActivityGrainDto>
            {
                Success = false
            };
        }

        return new GrainResultDto<UserActivityGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<UserActivityState, UserActivityGrainDto>(State)
        };
    }
}