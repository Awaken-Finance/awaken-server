using Orleans;

namespace AwakenServer.Grains.Grain.Activity;

public interface IJoinRecordGrain : IGrainWithStringKey
{
    Task<GrainResultDto<JoinRecordGrainDto>> GetAsync();
    Task<GrainResultDto<JoinRecordGrainDto>> AddOrUpdateAsync(JoinRecordGrainDto grainDto);
}