using Orleans;

namespace AwakenServer.Grains.Grain.MyPortfolio;

public interface ICurrentTradePairGrain : IGrainWithStringKey
{
    Task<GrainResultDto<CurrentTradePairGrainDto>> AddTotalSupplyAsync(long lpTokenAmount);
    Task<GrainResultDto<CurrentTradePairGrainDto>> AddTotalFeeAsync(long total0Fee, long total1Fee);
    Task<GrainResultDto<CurrentTradePairGrainDto>> GetAsync();
}