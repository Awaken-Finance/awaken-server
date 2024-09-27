using AwakenServer.Grains.State.Activity;
using AwakenServer.Grains.State.MyPortfolio;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Index;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Grains.Grain.Activity;

public class ActivityRankingSnapshotGrain : Grain<ActivityRankingSnapshotState>, IActivityRankingSnapshotGrain
{
    private readonly IObjectMapper _objectMapper;
    public ActivityRankingSnapshotGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public async Task<GrainResultDto<ActivityRankingSnapshotGrainDto>> AddOrUpdateAsync(
        ActivityRankingSnapshotGrainDto dto)
    {
        if (State.Id == Guid.Empty)
        {
            State.Id = Guid.NewGuid();
        }
        
        _objectMapper.Map(dto, State);
        await WriteStateAsync();
        return new GrainResultDto<ActivityRankingSnapshotGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<ActivityRankingSnapshotState, ActivityRankingSnapshotGrainDto>(State)
        };
    }
}