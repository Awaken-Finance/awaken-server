using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Index;
using Orleans;

namespace AwakenServer.Grains.Grain.MyPortfolio;

public interface ICurrentUserLiquidityGrain : IGrainWithStringKey
{
    Task<GrainResultDto<CurrentUserLiquidityGrainDto>> GetAsync();
    Task<GrainResultDto<CurrentUserLiquidityGrainDto>> AddLiquidityAsync(LiquidityRecordDto liquidityRecordDto,
        Guid tradePairId, string token0Symbol);
    Task<GrainResultDto<CurrentUserLiquidityGrainDto>> RemoveLiquidityAsync(LiquidityRecordDto liquidityRecordDto,
        Guid tradePairId, string token0Symbol);
    Task<GrainResultDto<CurrentUserLiquidityGrainDto>> AddTotalFee(long total0Fee, long total1Fee, SwapRecordDto swapRecordDto);
}