using Orleans;

namespace AwakenServer.Grains.Grain.StatInfo;

public interface ITokenStatInfoGrain : IGrainWithStringKey
{
    Task<GrainResultDto<TokenStatInfoGrainDto>> AddOrUpdateAsync(TokenStatInfoGrainDto dto);
    Task<GrainResultDto<TokenStatInfoGrainDto>> GetAsync();
}