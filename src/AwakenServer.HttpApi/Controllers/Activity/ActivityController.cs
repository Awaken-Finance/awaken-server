using System.Threading.Tasks;
using AwakenServer.Activity;
using AwakenServer.Activity.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace AwakenServer.Controllers.Activity;

[RemoteService]
[Area("app")]
[ControllerName("Asset")]
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
    public virtual async Task JoinAsync(JoinInput input)
    {
        await _activityAppService.JoinAsync(input);
    }
    
    [HttpGet]
    [Route("join-status")]
    public virtual async Task JoinStatusAsync(GetJoinStatusInput input)
    {
        await _activityAppService.GetJoinStatusAsync(input);
    }
    
    [HttpGet]
    [Route("my-ranking")]
    public virtual async Task MyRankingAsync(GetMyRankingInput input)
    {
        await _activityAppService.GetMyRankingAsync(input);
    }
    
    [HttpGet]
    [Route("ranking-list")]
    public virtual async Task RankingListAsync(ActivityBaseDto input)
    {
        await _activityAppService.GetRankingListAsync(input);
    }
}