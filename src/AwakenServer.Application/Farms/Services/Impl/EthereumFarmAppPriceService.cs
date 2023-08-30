using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Price;
using AwakenServer.Price.Dtos;

namespace AwakenServer.Farms.Services.Impl
{
    public class EthereumFarmAppPriceService : IFarmAppPriceService//, ITransientDependency todo configure price service
    {
        private readonly IFarmPriceAppService _farmPriceAppService;
        private readonly IPriceAppService _priceAppService;

        public EthereumFarmAppPriceService(IFarmPriceAppService farmPriceAppService, IPriceAppService priceAppService)
        {
            _farmPriceAppService = farmPriceAppService;
            _priceAppService = priceAppService;
        }

        public async Task<List<FarmPriceDto>> GetSwapTokenPricesAsync(GetSwapTokenPricesInput input)
        {
            return await _farmPriceAppService.GetPricesAsync(new GetFarmTokenPriceInput
            {
                ChainId = input.ChainId,
                TokenAddresses = input.TokenAddresses
            });
        }

        public async Task<string> GetTokenPriceAsync(GetTokenPriceInput input)
        {
            return await _priceAppService.GetTokenPriceAsync(input);
        }
    }
}