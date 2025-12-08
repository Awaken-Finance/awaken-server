using Orleans;

namespace AwakenServer.Grains.Grain.MyPortfolio;

public interface ICurrentTradePairGrain : IGrainWithStringKey
{
    Task<GrainResultDto<CurrentTradePairGrainDto>> AddTotalSupplyAsync(Guid tradePairId, long lpTokenAmount, long timestamp);
    Task<GrainResultDto<CurrentTradePairGrainDto>> AddTotalFeeAsync(Guid tradePairId, long total0Fee, long total1Fee);
    Task<GrainResultDto<CurrentTradePairGrainDto>> GetAsync();
}