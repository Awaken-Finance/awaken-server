using System.Threading.Tasks;
using Orleans;

namespace AwakenServer.Grains.Grain.Trade;

public interface ILiquidityRecordGrain : IGrainWithStringKey
{
    public Task AddAsync(LiquidityRecordGrainDto liquidityRecord);
    public Task<GrainResultDto<LiquidityRecordGrainDto>> GetAsync();
    public Task<bool> ExistAsync();
}