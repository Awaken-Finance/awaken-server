using AwakenServer.Grains.State.Chain;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Grains.Grain.Chain;
public class ChainGrain : Grain<ChainState>, IChainGrain
{
   private readonly IObjectMapper _objectMapper;
    
   public ChainGrain(IObjectMapper objectMapper)
   {
      _objectMapper = objectMapper;
   }

   public async Task<GrainResultDto<ChainGrainDto>> AddChainAsync(ChainGrainDto chain)
   { 
       if (chain.IsEmpty())
       {
           return new GrainResultDto<ChainGrainDto>
           {
               Success = false,
               Data = new ChainGrainDto()
           };
       }
       State = _objectMapper.Map<ChainGrainDto, ChainState>(chain);
       await WriteStateAsync();
       return new GrainResultDto<ChainGrainDto>
       {
           Success = true,
           Data = _objectMapper.Map<ChainState, ChainGrainDto>(State)
       };
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

    public async Task<ChainGrainDto> GetByIdAsync(string Id)
    {
        if (string.IsNullOrEmpty(Id))
        {
            return null;
        }

        return await Task.FromResult(_objectMapper.Map<ChainState, ChainGrainDto>(State));
    }

    public async Task SetBlockHeightAsync(long latestBlockHeight, long latestBlockHeightExpireMs)
    {
        if (latestBlockHeight < State.LatestBlockHeight)
        {
            return;
        }

        State.LatestBlockHeight = latestBlockHeight;
        State.LatestBlockHeightExpireMs = latestBlockHeightExpireMs;
        await WriteStateAsync();
    }

    public async Task SetNameAsync(string name)
    {
        State.Name = name;
        await WriteStateAsync();
    }

    public async Task SetChainIdAsync(int chainId)
    {
        State.AElfChainId = chainId;
        await WriteStateAsync();
    }
}