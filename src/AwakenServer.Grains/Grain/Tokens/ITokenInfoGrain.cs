using System;
using System.Threading.Tasks;
using AwakenServer.Tokens;
using Orleans;

namespace AwakenServer.Grains.Grain.Tokens;

public interface ITokenInfoGrain : IGrainWithStringKey
{
    Task<GrainResultDto<TokenGrainDto>> CreateAsync(TokenCreateDto input);
    
    Task<GrainResultDto<TokenGrainDto>> GetByIdAsync(Guid Id);
}