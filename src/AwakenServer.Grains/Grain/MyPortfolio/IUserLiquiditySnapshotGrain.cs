using Orleans;

namespace AwakenServer.Grains.Grain.MyPortfolio;

public interface IUserLiquiditySnapshotGrain : IGrainWithStringKey
{
    Task<GrainResultDto<UserLiquiditySnapshotGrainDto>> AddOrUpdateAsync(UserLiquiditySnapshotGrainDto dto);
    Task<GrainResultDto<UserLiquiditySnapshotGrainDto>> GetAsync();
}