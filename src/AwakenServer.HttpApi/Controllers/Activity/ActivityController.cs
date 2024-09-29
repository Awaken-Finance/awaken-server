using System.Threading.Tasks;
using AwakenServer.Activity;
using AwakenServer.Activity.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace AwakenServer.Controllers.Activity;

[RemoteService]
[Area("app")]
[ControllerName("Activity")]
[Route("api/app/activity")]
public class ActivityController : AbpController
{
    private readonly IActivityAppService _activityAppService;

    public ActivityController(IActivityAppService activityAppService)
    {
        _activityAppService = activityAppService;
    }
    
    [HttpPost]
    [Route("join")]
    public virtual async Task<string> JoinAsync(JoinInput input)
    {
        return await _activityAppService.JoinAsync(input);
    }
    
    [HttpGet]
    [Route("join-status")]
    public virtual async Task<JoinStatusDto> JoinStatusAsync(GetJoinStatusInput input)
    {
        return await _activityAppService.GetJoinStatusAsync(input);
    }
    
    [HttpGet]
    [Route("my-ranking")]
    public virtual async Task<MyRankingDto> MyRankingAsync(GetMyRankingInput input)
    {
       return await _activityAppService.GetMyRankingAsync(input);
    }
    
    [HttpGet]
    [Route("ranking-list")]
    public virtual async Task<RankingListDto> RankingListAsync(ActivityBaseDto input)
    {
        return await _activityAppService.GetRankingListAsync(input);
    }
}