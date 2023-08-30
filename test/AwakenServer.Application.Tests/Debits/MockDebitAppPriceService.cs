using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwakenServer.Debits.Services;
using AwakenServer.Price.Dtos;

namespace AwakenServer.Debits
{
    public class MockDebitAppPriceService: IDebitAppPriceService
    {
        public async Task<List<UnderlyingTokenPriceDto>> GetCTokenPricesAsync(GetTokenPricesInput input)
        {
            return input.TokenAddresses.Select((address, index) => new UnderlyingTokenPriceDto
            {
                ChainId = input.ChainId,
                TokenAddress = address,
                TokenSymbol = input.TokenSymbol[index],
                Price = "1"
            }).ToList();
        }

        public async Task<List<UnderlyingTokenPriceDto>> GetUnderlyingTokenPricesAsync(GetTokenPricesInput input)
        {
            return input.TokenAddresses.Select((address, index) => new UnderlyingTokenPriceDto
            {
                ChainId = input.ChainId,
                TokenAddress = address,
                TokenSymbol = input.TokenSymbol[index],
                Price = "1"
            }).ToList();
        }

        public Task<string> GetTokenPricesAsync(GetTokenPriceInput input)
        {
            return Task.FromResult("1");
        }
    }
}