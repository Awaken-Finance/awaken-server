using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Activity.Dtos;
using AwakenServer.Trade.Dtos;
using Volo.Abp.Application.Services;

namespace AwakenServer.Activity;

public interface IActivityAppService : IApplicationService
{
    Task<string> JoinAsync(JoinInput input);
    Task<JoinStatusDto> GetJoinStatusAsync(GetJoinStatusInput input);
    Task<MyRankingDto> GetMyRankingAsync(GetMyRankingInput input);
    Task<RankingListDto> GetRankingListAsync(ActivityBaseDto input);
    Task<bool> CreateSwapAsync(SwapRecordDto dto);
    Task<bool> CreateLimitOrderFillRecordAsync(LimitOrderFillRecordDto dto);
    Task<bool> CreateLpSnapshotAsync(long executeTime, string type);
    Task<bool> CreateSwapAsync(SwapRecordDto dto, List<Activity> ActivityList);
    Task<bool> CreateLimitOrderFillRecordAsync(LimitOrderFillRecordDto dto, List<Activity> ActivityList);
    Task<bool> CreateLpSnapshotAsync(long executeTime, string type, List<Activity> ActivityList);
}