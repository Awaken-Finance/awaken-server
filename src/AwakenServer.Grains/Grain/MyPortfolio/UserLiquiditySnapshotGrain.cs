using AwakenServer.Grains.State.MyPortfolio;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Grains.Grain.MyPortfolio;

public class UserLiquiditySnapshotGrain : Grain<UserLiquiditySnapshotState>, IUserLiquiditySnapshotGrain
{
    private readonly IObjectMapper _objectMapper;
    public UserLiquiditySnapshotGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public Task<GrainResultDto<UserLiquiditySnapshotGrainDto>> AddOrUpdateAsync(UserLiquiditySnapshotGrainDto dto)
    {
        return null;
    }

    public async Task<GrainResultDto<UserLiquiditySnapshotGrainDto>> GetAsync()
    {
        await ReadStateAsync();
        var result = new GrainResultDto<UserLiquiditySnapshotGrainDto>();
        if (State.TradePairId == Guid.Empty)
        {
            result.Success = false;
            return result;
        }
        result.Data = _objectMapper.Map<UserLiquiditySnapshotState, UserLiquiditySnapshotGrainDto>(State);
        result.Success = true;
        return result;
    }
}