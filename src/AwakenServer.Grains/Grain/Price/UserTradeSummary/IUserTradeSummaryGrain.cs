using Orleans;

namespace AwakenServer.Grains.Grain.Price.UserTradeSummary;

public interface IUserTradeSummaryGrain : IGrainWithStringKey
{
    Task<GrainResultDto<UserTradeSummaryGrainDto>> GetAsync();
    Task<GrainResultDto<UserTradeSummaryGrainDto>> AddOrUpdateAsync(UserTradeSummaryGrainDto dto);
}