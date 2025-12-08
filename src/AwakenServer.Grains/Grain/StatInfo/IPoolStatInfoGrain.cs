using Orleans;

namespace AwakenServer.Grains.Grain.StatInfo;

public interface IPoolStatInfoGrain : IGrainWithStringKey
{
    Task<GrainResultDto<PoolStatInfoGrainDto>> AddOrUpdateAsync(PoolStatInfoGrainDto grainDto);
    Task<GrainResultDto<PoolStatInfoGrainDto>> GetAsync();
}