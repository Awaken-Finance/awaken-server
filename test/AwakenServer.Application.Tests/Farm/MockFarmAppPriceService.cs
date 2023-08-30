using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwakenServer.Farms.Services;
using AwakenServer.Price.Dtos;

namespace AwakenServer.Farm
{
    public class MockFarmAppPriceService: IFarmAppPriceService
    {
        public async Task<List<FarmPriceDto>> GetSwapTokenPricesAsync(GetSwapTokenPricesInput input)
        {
            return input.TokenAddresses.Select((address, index) => new FarmPriceDto
            {
                ChainId = input.ChainId,
                TokenAddress = address,
                TokenSymbol = input.TokenSymbol[index],
                Price = "1"
            }).ToList();
        }

        public Task<string> GetTokenPriceAsync(GetTokenPriceInput input)
        {
            return Task.FromResult("1");
        }
    }
}