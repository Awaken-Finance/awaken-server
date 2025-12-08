using System.Threading.Tasks;
using Orleans;

namespace AwakenServer.Grains.Grain.Trade;

public interface IUserTradeSummaryGrain : IGrainWithStringKey
{
    Task<GrainResultDto<UserTradeSummaryGrainDto>> GetAsync();
    Task<GrainResultDto<UserTradeSummaryGrainDto>> AddOrUpdateAsync(UserTradeSummaryGrainDto dto);
}

