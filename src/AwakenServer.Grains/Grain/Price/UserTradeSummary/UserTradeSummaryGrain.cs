using AwakenServer.Grains.State.Price;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Grains.Grain.Price.UserTradeSummary;

public class UserTradeSummaryGrain : Grain<UserTradeSummaryState>, IUserTradeSummaryGrain
{
    private readonly IObjectMapper _objectMapper;

    public UserTradeSummaryGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }

    public async Task<GrainResultDto<UserTradeSummaryGrainDto>> GetAsync()
    {
        if (State.Id == Guid.Empty)
        {
            return new GrainResultDto<UserTradeSummaryGrainDto>()
            {
                Success = false
            };
        }

        return new GrainResultDto<UserTradeSummaryGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<UserTradeSummaryState, UserTradeSummaryGrainDto>(State)
        };
    }

    public async Task<GrainResultDto<UserTradeSummaryGrainDto>> AddOrUpdateAsync(UserTradeSummaryGrainDto dto)
    {
        State = _objectMapper.Map<UserTradeSummaryGrainDto, UserTradeSummaryState>(dto);
        await WriteStateAsync();

        return new GrainResultDto<UserTradeSummaryGrainDto>()
        {
            Success = true,
            Data = dto
        };
    }
}