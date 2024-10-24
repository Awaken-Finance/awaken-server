using Orleans;

namespace AwakenServer.Grains.Grain.StatInfo;

public interface IStatInfoSnapshotGrain : IGrainWithStringKey
{
    Task<GrainResultDto<StatInfoSnapshotGrainDto>> AddOrUpdateAsync(StatInfoSnapshotGrainDto dto);
}