using System;
using System.Threading.Tasks;
using Asp.Versioning;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace AwakenServer.Controllers.Trade
{
    [RemoteService]
    [Area("app")]
    [ControllerName("TradePair")]
    [Route("api/app/trade-pairs")]
    public class TradePairController : AbpController
    {
        private readonly ITradePairAppService _tradePairAppService;

        public TradePairController(ITradePairAppService tradePairAppService)
        {
            _tradePairAppService = tradePairAppService;
        }

        [HttpGet]
        public virtual Task<PagedResultDto<TradePairIndexDto>> GetListAsync(GetTradePairsInput input)
        {
            return _tradePairAppService.GetListAsync(input);
        }

        [HttpGet]
        [Route("{id}")]
        public virtual Task<TradePairIndexDto> GetAsync(Guid id, [FromQuery] [CanBeNull] string address)
        {
            return _tradePairAppService.GetByAddressAsync(id, address);
        }
        
        [HttpPost]
        [Route("by-ids")]
        public virtual Task<ListResultDto<TradePairIndexDto>> GetByIdsAsync(GetTradePairByIdsInput input)
        {
            return _tradePairAppService.GetByIdsFromGrainAsync(input);
        }
        
        [HttpGet]
        [Route("tokens")]
        public virtual Task<TokenListDto> GetTokenListAsync(GetTokenListInput input)
        {
            return _tradePairAppService.GetTokenListAsync(input);
        }
        
        [HttpGet]
        [Route("test/latest-snapshot/{id}")]
        public virtual Task<TradePairMarketDataSnapshotDto> GetTradePairLatestSnapshotAsync(Guid id)
        {
            return _tradePairAppService.GetTradePairLatestSnapshotAsync(id);
        }
    }
}