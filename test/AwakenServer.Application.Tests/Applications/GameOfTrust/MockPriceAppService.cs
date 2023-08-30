using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Price;
using AwakenServer.Price.Dtos;
using AwakenServer.Tokens.Dtos;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Applications.GameOfTrust
{
    public class MockPriceAppService : IPriceAppService
    {
        public Task<string> GetTokenPriceAsync(GetTokenPriceInput input)
        {
            switch (input.Symbol)
            {
                case "SASHIMI": return Task.FromResult("1");
                case "ISTAR" : return Task.FromResult("1");
                case "BTC": return Task.FromResult("69000");
                case "USDT": return Task.FromResult("6");
            }
            return Task.FromResult("1");
        }

        public async Task<ListResultDto<TokenPriceDataDto>> GetTokenPriceListAsync(List<string> symbols)
        {
            return new ListResultDto<TokenPriceDataDto>();
        }

        public async Task<ListResultDto<TokenPriceDataDto>> GetTokenHistoryPriceDataAsync(
            List<GetTokenHistoryPriceInput> inputs)
        {
            return new ListResultDto<TokenPriceDataDto>();
        }
    }
}