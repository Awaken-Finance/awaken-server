using System.Threading.Tasks;
using Asp.Versioning;
using AwakenServer.Asset;
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
    [ControllerName("Liquidity")]
    [Route("api/app/liquidity")]

    public class LiquidityController : AbpController
    {
        private readonly ILiquidityAppService _liquidityAppService;
        private readonly IMyPortfolioAppService _myPortfolioAppService;

        public LiquidityController(ILiquidityAppService liquidityAppService,
            IMyPortfolioAppService myPortfolioAppService)
        {
            _liquidityAppService = liquidityAppService;
            _myPortfolioAppService = myPortfolioAppService;

        }

        [HttpGet]
        [Route("liquidity-records")]
        public virtual Task<PagedResultDto<LiquidityRecordIndexDto>> GetRecordsAsync(GetLiquidityRecordsInput input)
        {
            return _liquidityAppService.GetRecordsAsync(input);
        }
        
        [HttpGet]
        [Route("user-liquidity")]
        public virtual Task<PagedResultDto<UserLiquidityIndexDto>> GetUserLiquidityAsync(GetUserLiquidityInput input)
        {
            return _liquidityAppService.GetUserLiquidityFromGraphQLAsync(input);
        }
        
        [HttpGet]
        [Route("user-asset")]
        public virtual Task<UserAssetDto> GetUserAssetAsync(GetUserAssetInput input)
        {
            return _liquidityAppService.GetUserAssetFromGraphQLAsync(input);
        }
        
        [HttpGet]
        [Route("user-positions")]
        public virtual async Task<PagedResultDto<TradePairPositionDto>> UserPositionsAsync(GetUserPositionsDto input)
        {
            return await _myPortfolioAppService.GetUserPositionsAsync(input);
        }
    }
}