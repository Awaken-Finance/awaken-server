using System.Threading.Tasks;
using Asp.Versioning;
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
    [ControllerName("TradeRecord")]
    [Route("api/app/trade-records")]

    public class TradeRecordController : AbpController
    {
        private readonly ITradeRecordAppService _tradeRecordAppService;

        public TradeRecordController(ITradeRecordAppService tradeRecordAppService)
        {
            _tradeRecordAppService = tradeRecordAppService;
        }

        [HttpGet]
        public virtual Task<PagedResultDto<TradeRecordIndexDto>> GetListAsync(GetTradeRecordsInput input)
        {
            return _tradeRecordAppService.GetListAsync(input);
        }
    }
}