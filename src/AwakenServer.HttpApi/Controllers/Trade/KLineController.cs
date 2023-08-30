using System.Threading.Tasks;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace AwakenServer.Controllers.Trade
{
    [RemoteService]
    [Area("app")]
    [ControllerName("KLine")]
    [Route("api/app/k-lines")]

    public class KLineController : AbpController
    {
        private readonly IKLineAppService _kLineAppService;

        public KLineController(IKLineAppService kLineAppService)
        {
            _kLineAppService = kLineAppService;
        }

        [HttpGet]
        public virtual Task<ListResultDto<KLineDto>> GetListAsync(GetKLinesInput input)
        {
            return _kLineAppService.GetListAsync(input);
        }
    }
}