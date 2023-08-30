using Orleans;

namespace AwakenServer.Grains.Grain.Trade;

public interface IKLineGrain : IGrainWithStringKey
{
    Task<GrainResultDto<KLineGrainDto>> AddOrUpdateAsync(KLineGrainDto kLineGrainDto);
    Task<GrainResultDto<KLineGrainDto>> GetAsync();
}