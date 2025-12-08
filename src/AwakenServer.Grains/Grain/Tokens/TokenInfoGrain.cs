using System;
using System.Threading.Tasks;
using AwakenServer.Grains.State.Tokens;
using AwakenServer.Tokens;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Grains.Grain.Tokens;

public class TokenInfoGrain : Grain<TokenInfoState>, ITokenInfoGrain
{
    private readonly IObjectMapper _objectMapper;

    public TokenInfoGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    public async Task<GrainResultDto<TokenGrainDto>> CreateAsync(TokenCreateDto input)
    {
        if (!State.IsEmpty())
        {
            return new GrainResultDto<TokenGrainDto>
            {
                Success = true,
                Data = _objectMapper.Map<TokenInfoState, TokenGrainDto>(State)
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
        State = _objectMapper.Map<TokenCreateDto, TokenInfoState>(input);
        await WriteStateAsync();
        return new GrainResultDto<TokenGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<TokenInfoState, TokenGrainDto>(State),
        };
    }

    public async Task<GrainResultDto<TokenGrainDto>> GetAsync()
    {
        if (State.IsEmpty())
        {
            return new GrainResultDto<TokenGrainDto>
            {
                Success = false,
                Data = new TokenGrainDto(),
            };
        }

        var result = new GrainResultDto<TokenGrainDto>();
        result.Success = true;
        result.Data = _objectMapper.Map<TokenInfoState, TokenGrainDto>(State);
        return await Task.FromResult(result);
    }
}