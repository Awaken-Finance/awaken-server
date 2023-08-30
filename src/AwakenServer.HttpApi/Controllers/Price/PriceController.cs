using System.Threading.Tasks;
using AwakenServer.Price;
using AwakenServer.Price.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace AwakenServer.Controllers.Price
{
    [RemoteService]
    [Area("app")]
    [ControllerName("Price")]
    [Route("api/app")]
    public class PriceController : AbpController
    {
        // private readonly ILendingTokenPriceAppService _lendingTokenPriceAppService;
        // private readonly IFarmPriceAppService _farmPriceAppService;
        private readonly IPriceAppService _priceAppService;

        public PriceController(IPriceAppService priceAppService)
        {
            // _lendingTokenPriceAppService = lendingTokenPriceAppService;
            // _farmPriceAppService = farmPriceAppService;
            _priceAppService = priceAppService;
        }

        // [HttpGet]
        // [Route("lending/prices")]
        // public virtual async Task<List<LendingTokenPriceIndexDto>> GetPricesAsync(GetPricesInput input)
        // {
        //     if (input.TokenIds.Length > LimitedResultRequestDto.MaxMaxResultCount)
        //     {
        //         throw new UserFriendlyException("Invalid tokenId count");
        //     }
        //
        //     return await _lendingTokenPriceAppService.GetPricesAsync(input);
        // }
        //
        // [HttpGet]
        // [Route("lending/price-history")]
        // public virtual Task<PagedResultDto<LendingTokenPriceHistoryIndexDto>> GetPriceHistoryAsync(
        //     GetPriceHistoryInput input)
        // {
        //     return _lendingTokenPriceAppService.GetPriceHistoryAsync(input);
        // }
        //
        // [HttpGet]
        // [Route("farm/prices")]
        // public virtual async Task<List<FarmPriceDto>> GetFarmTokenPriceAsync(GetFarmTokenPriceInput input)
        // {
        //     return await _farmPriceAppService.GetPricesAsync(input);
        // }

        [HttpGet]
        [Route("token/price")]
        public virtual async Task<string> GetTokenPriceAsync(GetTokenPriceInput input)
        {
            return await _priceAppService.GetTokenPriceAsync(input);
        }
    }
}