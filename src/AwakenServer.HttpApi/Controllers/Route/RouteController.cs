using System.Threading.Tasks;
using Asp.Versioning;
using AwakenServer.Route;
using AwakenServer.Route.Dtos;
using AwakenServer.SwapTokenPath;
using AwakenServer.SwapTokenPath.Dtos;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace AwakenServer.Controllers.Route 
{
    [RemoteService]
    [Area("app")]
    [ControllerName("Route")]
    [Route("api/app/route")]

    public class RouteController : AbpController
    {
        private readonly IBestRoutesAppService _routeAppService;

        public RouteController(IBestRoutesAppService routeAppService)
        {
            _routeAppService = routeAppService;
        }

        [HttpGet]
        [Route("best-swap-routes")]
        public virtual Task<BestRoutesDto> GetBestRoutesAsync(GetBestRoutesInput input)
        {
            return _routeAppService.GetBestRoutesAsync(input);
        }
    }
}