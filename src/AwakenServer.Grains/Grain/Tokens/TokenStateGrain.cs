using AwakenServer.Grains.State.Tokens;
using AwakenServer.Tokens;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Grains.Grain.Tokens;

public class TokenStateGrain : Grain<TokenState>, ITokenStateGrain
{
    private readonly IObjectMapper _objectMapper;

    public TokenStateGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
        
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }

    public async Task<GrainResultDto<TokenGrainDto>> CreateAsync(TokenCreateDto input)
    {
        if (!State.IsEmpty())
        {
            return new GrainResultDto<TokenGrainDto>
            {
                Success = true,
                Data = _objectMapper.Map<TokenState, TokenGrainDto>(State)
            };
        }

        if (input.IsEmpty())
        {
            return new GrainResultDto<TokenGrainDto>
            {
                Success = false,
                Data = new TokenGrainDto(),
            };
        }
        State = _objectMapper.Map<TokenCreateDto, TokenState>(input);
        await WriteStateAsync();
        return new GrainResultDto<TokenGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<TokenState, TokenGrainDto>(State),
        };
    }

    public async Task<GrainResultDto<TokenGrainDto>> GetByIdAsync(Guid Id)
    {
        if (Id == Guid.Empty)
        {
            return new GrainResultDto<TokenGrainDto>
            {
                Success = false,
                Data = new TokenGrainDto(),
            };
        }

        var result = new GrainResultDto<TokenGrainDto>();
        result.Success = true;
        result.Data = _objectMapper.Map<TokenState, TokenGrainDto>(State);
        return await Task.FromResult(result);
    }
}