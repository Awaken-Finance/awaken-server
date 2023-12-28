using AwakenServer.Grains.State.Asset;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Grains.Grain.Asset;

public class DefaultTokenGrain : Grain<DefaultTokenState>, IDefaultTokenGrain
{
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


    public async Task<GrainResultDto> SetTokenAsync(string symbol)
    {
        State.Symbol = symbol;

        await WriteStateAsync();
        var result = new GrainResultDto();
        result.Success = true;
        return result;
    }

    public async Task<GrainResultDto<DefaultTokenGrainDto>> GetAsync()
    {
        var result = new GrainResultDto<DefaultTokenGrainDto>();

        result.Success = true;
        result.Data = new DefaultTokenGrainDto()
        {
            TokenSymbol = State.Symbol,
        };

        return result;
    }
}