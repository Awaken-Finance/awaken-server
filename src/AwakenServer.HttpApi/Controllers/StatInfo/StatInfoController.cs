using System.Threading.Tasks;
using AwakenServer.StatInfo;
using AwakenServer.StatInfo.Dtos;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace AwakenServer.Controllers.StatInfo
{
    [RemoteService]
    [Area("app")]
    [ControllerName("Info")]
    [Route("/api/app/info")]

    public class StatInfoController : AbpController
    {
        private readonly IStatInfoAppService _statInfoAppService;

        public StatInfoController(IStatInfoAppService statInfoAppService)
        {
            _statInfoAppService = statInfoAppService;
        }

        [HttpGet]
        [Route("tvl/history")]
        public virtual Task<ListResultDto<StatInfoTvlDto>> GetListAsync(GetStatHistoryInput input)
        {
            return _statInfoAppService.GetTvlListAsync(input);
        }
    }
}