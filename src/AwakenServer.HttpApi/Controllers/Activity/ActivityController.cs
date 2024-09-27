using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace AwakenServer.Controllers.Activity;

[RemoteService()]
[Area("app")]
[ControllerName("Asset")]
[Route("api/app/activity")]
public class ActivityController : AbpController
{
    
}