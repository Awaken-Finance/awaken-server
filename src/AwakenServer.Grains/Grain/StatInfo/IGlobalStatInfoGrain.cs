using Orleans;

namespace AwakenServer.Grains.Grain.StatInfo;

public interface IGlobalStatInfoGrain : IGrainWithStringKey
{
    Task<GrainResultDto<GlobalStatInfoGrainDto>> AddTvlAsync(double tvl);
    Task<GrainResultDto<GlobalStatInfoGrainDto>> UpdateTvlAsync(double tvl);
    Task<GrainResultDto<GlobalStatInfoGrainDto>> GetAsync();
}