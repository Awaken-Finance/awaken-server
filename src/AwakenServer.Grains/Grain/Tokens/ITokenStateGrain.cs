using AwakenServer.Tokens;
using Orleans;

namespace AwakenServer.Grains.Grain.Tokens;

public interface ITokenStateGrain:IGrainWithGuidKey
{
    Task<GrainResultDto<TokenGrainDto>> CreateAsync(TokenCreateDto input);
    
    Task<GrainResultDto<TokenGrainDto>> GetByIdAsync(Guid Id);
}