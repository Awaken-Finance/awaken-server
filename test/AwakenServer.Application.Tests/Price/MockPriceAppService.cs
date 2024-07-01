using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwakenServer.Price;
using AwakenServer.Price.Dtos;
using AwakenServer.Tokens.Dtos;
using MongoDB.Driver.Linq;
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
                case "ISTAR": return Task.FromResult("1");
                case "BTC": return Task.FromResult("69000");
                case "USDT": return Task.FromResult("6");
            }

            return Task.FromResult("0");
        }

        public async Task<ListResultDto<TokenPriceDataDto>> GetTokenPriceListAsync(List<string> symbols)
        {
            return new ListResultDto<TokenPriceDataDto>()
            {
                Items = new List<TokenPriceDataDto>()
                {
                    new TokenPriceDataDto()
                    {
                        Symbol = "USDT",
                        PriceInUsd = 1
                    },
                    new TokenPriceDataDto()
                    {
                        Symbol = "BTC",
                        PriceInUsd = 1
                    },
                    new TokenPriceDataDto()
                    {
                        Symbol = "EOS",
                        PriceInUsd = 1
                    }
                }.Where(o => symbols.Contains(o.Symbol)).ToList()
            };
        }

        public async Task<ListResultDto<TokenPriceDataDto>> GetTokenHistoryPriceDataAsync(
            List<GetTokenHistoryPriceInput> inputs)
        {
            return new ListResultDto<TokenPriceDataDto>();
        }
        
        public async Task RebuildPricingMapAsync(string chainId)
        {
            return;
        }

        public async Task UpdateAffectedPriceMapAsync(string chainId, Guid tradePairId, string token0Amount,
            string token1Amount)
        {
            return;
        }
    }
}