using System;
using System.Threading.Tasks;
using AwakenServer.Grains.State.Price;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Grains.Grain.Trade;

public class UserTradeSummaryGrain : Grain<UserTradeSummaryState>, IUserTradeSummaryGrain
{
    private readonly IObjectMapper _objectMapper;

    public UserTradeSummaryGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, cancellationToken);
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