using AwakenServer.Grains.State.StatInfo;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Grains.Grain.StatInfo;

public class GlobalStatInfoGrain : Grain<GlobalStatInfoState>, IGlobalStatInfoGrain
{
    private readonly IObjectMapper _objectMapper;

    public GlobalStatInfoGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public async Task<GrainResultDto<GlobalStatInfoGrainDto>> AddTvlAsync(double tvl)
    {
        State.Tvl += tvl;
        await WriteStateAsync();
        return new GrainResultDto<GlobalStatInfoGrainDto>
        {
            Data = _objectMapper.Map<GlobalStatInfoState, GlobalStatInfoGrainDto>(State),
            Success = false
        };
    }

    public async Task<GrainResultDto<GlobalStatInfoGrainDto>> UpdateTvlAsync(double tvl)
    {
        State.Tvl = tvl;
        await WriteStateAsync();
        return new GrainResultDto<GlobalStatInfoGrainDto>
        {
            Data = _objectMapper.Map<GlobalStatInfoState, GlobalStatInfoGrainDto>(State),
            Success = false
        };
    }

    public async Task<GrainResultDto<GlobalStatInfoGrainDto>> GetAsync()
    {
        await ReadStateAsync();
        return new GrainResultDto<GlobalStatInfoGrainDto>
        {
            Data = _objectMapper.Map<GlobalStatInfoState, GlobalStatInfoGrainDto>(State),
            Success = true
        };
    }
}