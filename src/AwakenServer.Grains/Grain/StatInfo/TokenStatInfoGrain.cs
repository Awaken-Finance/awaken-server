using AwakenServer.Grains.State.StatInfo;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Grains.Grain.StatInfo;

public class TokenStatInfoGrain : Grain<TokenStatInfoState>, ITokenStatInfoGrain
{
    private readonly IObjectMapper _objectMapper;

    public TokenStatInfoGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public async Task<GrainResultDto<TokenStatInfoGrainDto>> AddOrUpdateAsync(TokenStatInfoGrainDto dto)
    {
        _objectMapper.Map(dto, State);
        await WriteStateAsync();
        return new GrainResultDto<TokenStatInfoGrainDto>
        {
            Data = _objectMapper.Map<TokenStatInfoState, TokenStatInfoGrainDto>(State),
            Success = true
        };
    }

    public async Task<GrainResultDto<TokenStatInfoGrainDto>> GetAsync()
    {
        await ReadStateAsync();
        return new GrainResultDto<TokenStatInfoGrainDto>
        {
            Data = _objectMapper.Map<TokenStatInfoState, TokenStatInfoGrainDto>(State),
            Success = true
        };
    }
}