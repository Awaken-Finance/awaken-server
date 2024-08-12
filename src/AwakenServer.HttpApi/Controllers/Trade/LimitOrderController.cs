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
    [ControllerName("LimitOrder")]
    [Route("api/app/limit-order")]

    public class LimitOrderController : AbpController
    {
        private readonly ILimitOrderAppService _LimitOrderAppService;

        public LimitOrderController(ILimitOrderAppService LimitOrderAppService)
        {
            _LimitOrderAppService = LimitOrderAppService;
        }

        [HttpGet]
        [Route("my-orders")]
        public virtual Task<PagedResultDto<LimitOrderIndexDto>> GetListAsync(GetLimitOrdersInput input)
        {
            return _LimitOrderAppService.GetListAsync(input);
        }
        
        [HttpGet]
        [Route("fill-detail")]
        public virtual Task<PagedResultDto<LimitOrderFillRecordIndexDto>> GetListAsync(GetLimitOrderDetailsInput input)
        {
            return _LimitOrderAppService.GetListAsync(input);
        }
    }
}