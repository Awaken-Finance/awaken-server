using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwakenServer.Price;
using AwakenServer.Price.Dtos;

namespace AwakenServer.Debits.Services.Impl
{
    public class
        EthereumDebitAppPriceService : IDebitAppPriceService //, ITransientDependency todo configure price service
    {
        private readonly IFarmPriceAppService _farmPriceAppService;
        private readonly IPriceAppService _priceAppService;

        public EthereumDebitAppPriceService(IFarmPriceAppService farmPriceAppService, IPriceAppService priceAppService)
        {
            _farmPriceAppService = farmPriceAppService;
            _priceAppService = priceAppService;
        }

        public async Task<List<UnderlyingTokenPriceDto>> GetCTokenPricesAsync(GetTokenPricesInput input)
        {
            var priceDot = await _farmPriceAppService.GetPricesAsync(new GetFarmTokenPriceInput
            {
                ChainId = input.ChainId,
                TokenAddresses = input.TokenAddresses
            });
            return priceDot.Select(x => new UnderlyingTokenPriceDto
            {
                ChainId = x.ChainId,
                TokenSymbol = x.TokenSymbol,
                TokenAddress = x.TokenAddress,
                Price = x.Price
            }).ToList();
        }

        public async Task<List<UnderlyingTokenPriceDto>> GetUnderlyingTokenPricesAsync(GetTokenPricesInput input)
        {
            var ret = new List<UnderlyingTokenPriceDto>();
            for (var i = 0; i < input.TokenSymbol.Length; i++)
            {
                var symbol = input.TokenSymbol[i];
                var address = input.TokenAddresses[i];
                var price = await GetTokenPricesAsync(new GetTokenPriceInput
                {
                    ChainId = input.ChainId,
                    TokenAddress = address,
                    Symbol = symbol
                });
                ret.Add(new UnderlyingTokenPriceDto
                {
                    ChainId = input.ChainId,
                    TokenSymbol = symbol,
                    TokenAddress = address,
                    Price = price
                });
            }

            return ret;
        }

        public async Task<string> GetTokenPricesAsync(GetTokenPriceInput input)
        {
            return await _priceAppService.GetTokenPriceAsync(input);
        }
    }
}