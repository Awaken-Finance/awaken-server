using AwakenServer.Grains.State.StatInfo;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Grains.Grain.StatInfo;

public class PoolStatInfoGrain : Grain<PoolStatInfoState>, IPoolStatInfoGrain
{
    private readonly IObjectMapper _objectMapper;

    public PoolStatInfoGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public async Task<GrainResultDto<PoolStatInfoGrainDto>> AddOrUpdateAsync(PoolStatInfoGrainDto grainDto)
    {
        _objectMapper.Map(grainDto, State);
        if (State.Id == Guid.Empty)
        {
            State.Id = Guid.NewGuid();
        }
        await WriteStateAsync();
        return new GrainResultDto<PoolStatInfoGrainDto>
        {
            Data = _objectMapper.Map<PoolStatInfoState, PoolStatInfoGrainDto>(State),
            Success = true
        };
    }

    public async Task<GrainResultDto<PoolStatInfoGrainDto>> GetAsync()
    {
        await ReadStateAsync();
        return new GrainResultDto<PoolStatInfoGrainDto>
        {
            Data = _objectMapper.Map<PoolStatInfoState, PoolStatInfoGrainDto>(State),
            Success = true
        };
    }
}