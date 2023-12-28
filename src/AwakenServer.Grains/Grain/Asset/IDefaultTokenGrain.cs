using AwakenServer.Asset;
using AwakenServer.Grains.State.Asset;
using Orleans;

namespace AwakenServer.Grains.Grain.Asset;

public interface IDefaultTokenGrain : IGrainWithStringKey
{
    Task<GrainResultDto> SetTokenAsync(string symbol);
    Task<GrainResultDto<DefaultTokenGrainDto>> GetAsync();
}