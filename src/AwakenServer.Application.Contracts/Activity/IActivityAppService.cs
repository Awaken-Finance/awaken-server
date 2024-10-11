using System;
using System.Threading.Tasks;
using AwakenServer.Activity.Dtos;
using AwakenServer.Trade.Dtos;

namespace AwakenServer.Activity;

public interface IActivityAppService
{
    Task<string> JoinAsync(JoinInput input);
    Task<JoinStatusDto> GetJoinStatusAsync(GetJoinStatusInput input);
    Task<MyRankingDto> GetMyRankingAsync(GetMyRankingInput input);
    Task<RankingListDto> GetRankingListAsync(ActivityBaseDto input);
    Task<bool> CreateSwapAsync(SwapRecordDto dto);
    Task<bool> CreateLpSnapshotAsync(long executeTime, string type);
}