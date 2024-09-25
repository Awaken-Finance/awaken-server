using System.Threading.Tasks;
using AwakenServer.Activity.Dtos;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Activity;

public interface IActivityAppService
{
    Task JoinAsync(JoinInput input);
    Task<JoinStatusDto> GetJoinStatusAsync(GetJoinStatusInput input);
    Task<MyRankingDto> GetMyRankingAsync(GetMyRankingInput input);
    Task<PagedResultDto<RankingInfoDto>> GetRankingListAsync(ActivityBaseDto input);
}