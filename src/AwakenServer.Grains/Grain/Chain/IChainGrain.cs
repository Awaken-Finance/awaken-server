using Orleans;

namespace AwakenServer.Grains.Grain.Chain;

public interface IChainGrain : IGrainWithStringKey
{
    Task<GrainResultDto<ChainGrainDto>> AddChainAsync(ChainGrainDto chain);

    Task<ChainGrainDto> GetByIdAsync(string id);

    Task SetBlockHeightAsync(long latestBlockHeight, long latestBlockHeightExpireMs);

    Task SetNameAsync(string name);

    Task SetChainIdAsync(int chainId);
}