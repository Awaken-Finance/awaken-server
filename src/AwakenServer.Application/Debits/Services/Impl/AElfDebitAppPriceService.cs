using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwakenServer.Price.Dtos;
using AwakenServer.Tokens;
using AwakenServer.Trade;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.Debits.Services.Impl
{
    public class
        MockAElfDebitAppPriceService : IDebitAppPriceService, ITransientDependency // todo add AElf price service
    {
        public async Task<List<UnderlyingTokenPriceDto>> GetCTokenPricesAsync(GetTokenPricesInput input)
        {
            return input.TokenSymbol.Select((x, i) => new UnderlyingTokenPriceDto
            {
                TokenSymbol = x,
                TokenAddress = input.TokenAddresses[i],
                ChainId = input.ChainId,
                Price = "1"
            }).ToList();
        }

        public async Task<List<UnderlyingTokenPriceDto>> GetUnderlyingTokenPricesAsync(GetTokenPricesInput input)
        {
            return input.TokenSymbol.Select((x, i) => new UnderlyingTokenPriceDto
            {
                TokenSymbol = x,
                TokenAddress = input.TokenAddresses[i],
                ChainId = input.ChainId,
                Price = "1"
            }).ToList();
        }

        public Task<string> GetTokenPricesAsync(GetTokenPriceInput input)
        {
            return Task.FromResult("1");
        }
    }

    public class AElfDebitAppPriceService : IDebitAppPriceService //AElf price service
    {
        private readonly ITokenPriceProvider _tokenPriceProvider;
        private readonly ITokenAppService _tokenAppService;

        public AElfDebitAppPriceService(ITokenPriceProvider tokenPriceProvider, ITokenAppService tokenAppService,
            ITradePairAppService tradePairAppService)
        {
            _tokenPriceProvider = tokenPriceProvider;
            _tokenAppService = tokenAppService;
        }

        public async Task<List<UnderlyingTokenPriceDto>> GetCTokenPricesAsync(GetTokenPricesInput input)
        {
            var result = new List<UnderlyingTokenPriceDto>();
            for (var i = 0; i < input.TokenSymbol.Length; i++)
            {
                var tokenPrice = await GetTokenPriceAsync(input.ChainId, input.TokenSymbol[i]);
                result.Add(new UnderlyingTokenPriceDto
                {
                    ChainId = input.ChainId,
                    TokenSymbol = input.TokenSymbol[i],
                    TokenAddress = input.TokenAddresses[i],
                    Price = tokenPrice
                });
            }

            return result;
        }

        public async Task<List<UnderlyingTokenPriceDto>> GetUnderlyingTokenPricesAsync(GetTokenPricesInput input)
        {
            return await GetCTokenPricesAsync(input);
        }

        public async Task<string> GetTokenPricesAsync(GetTokenPriceInput input)
        {
            return (await GetCTokenPricesAsync(new GetTokenPricesInput
            {
                ChainId = input.ChainId,
                TokenSymbol = new[] { input.Symbol },
                TokenAddresses = new[] { input.TokenAddress }
            })).First().Price;
        }

        private async Task<string> GetTokenPriceAsync(string chainId, string symbol)
        {
            var token = await _tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = chainId,
                Symbol = symbol
            });

            if (token == null)
            {
                throw new Exception($"Lack of {symbol}-Usdt price");
            }

            return (await _tokenPriceProvider.GetTokenUSDPriceAsync(chainId, symbol)).ToString();
        }
    }
}