using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwakenServer.Grains.Grain.Tokens.TokenPrice;
using AwakenServer.Price.Dtos;
using AwakenServer.Tokens.Dtos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;

namespace AwakenServer.Price
{
    [RemoteService(IsEnabled = false)]
    public class PriceAppService : ApplicationService, IPriceAppService
    {
        private readonly IDistributedCache<PriceDto> _priceCache;
        private readonly ITokenPriceProvider _tokenPriceProvider;

        public PriceAppService(IDistributedCache<PriceDto> priceCache,
            ITokenPriceProvider tokenPriceProvider)
        {
            _priceCache = priceCache;
            _tokenPriceProvider = tokenPriceProvider;
        }

        public async Task<string> GetTokenPriceAsync(GetTokenPriceInput input)
        {
            if (string.IsNullOrWhiteSpace(input.Symbol)) return "0";
            var result = await GetTokenPriceListAsync(new List<string>{ input.Symbol });
            if (result.Items.Count == 0) return "0";
            else return result.Items[0].PriceInUsd.ToString();
        }
        
        public async Task<ListResultDto<TokenPriceDataDto>> GetTokenPriceListAsync(List<string> symbols)
        {
            var result = new List<TokenPriceDataDto>();
            if (symbols.Count == 0)
            {
                return new ListResultDto<TokenPriceDataDto>();
            }

            try
            {
                var symbolList = symbols.Distinct(StringComparer.InvariantCultureIgnoreCase).ToList();
                for (var i = 0; i < symbolList.Count; i++)
                {
                    var key = $"{PriceOptions.PriceCachePrefix}:{symbolList[i]}";
                    var price = await _priceCache.GetOrAddAsync(key,
                        async () => new PriceDto(), () => new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(PriceOptions.PriceExpirationTime + i)
                        });
                    if (price.PriceInUsd == PriceOptions.DefaultPriceValue)
                    {
                        price.PriceInUsd = await _tokenPriceProvider.GetPriceAsync(symbolList[i]);
                        await _priceCache.SetAsync(key, price, new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(PriceOptions.PriceExpirationTime + i)
                        });
                    }
                    
                    result.Add(new TokenPriceDataDto
                    {
                        Symbol = symbolList[i],
                        PriceInUsd = price.PriceInUsd
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Get price failed.");
                throw;
            }

            return new ListResultDto<TokenPriceDataDto>
            {
                Items = result
            };
        }

        public async Task<ListResultDto<TokenPriceDataDto>> GetTokenHistoryPriceDataAsync(
            List<GetTokenHistoryPriceInput> inputs)
        {
            var result = new List<TokenPriceDataDto>();
            try
            {
                foreach (var input in inputs)
                {
                    var time = input.DateTime.ToString("dd-MM-yyyy");
                    if (input.Symbol.IsNullOrEmpty())
                    {
                        result.Add(new TokenPriceDataDto());
                        continue;
                    }

                    var key = $"{PriceOptions.PriceHistoryCachePrefix}:{input.Symbol}:{time}";
                    var price = await _priceCache.GetOrAddAsync(key,
                        async () => new PriceDto(), () => new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(PriceOptions.PriceSuperLongExpirationTime)
                        });
                    if (price.PriceInUsd == PriceOptions.DefaultPriceValue || price.PriceInUsd == 0)
                    {
                        price.PriceInUsd = await _tokenPriceProvider.GetHistoryPriceAsync(input.Symbol, time);
                        await _priceCache.SetAsync(key, price, new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(PriceOptions.PriceSuperLongExpirationTime)
                        });
                    }
                    
                    Logger.LogInformation("Get history price, {symbol}, {time}, {priceInUsd}", input.Symbol, time, price.PriceInUsd);
                    result.Add(new TokenPriceDataDto
                    {
                        Symbol = input.Symbol,
                        PriceInUsd = price.PriceInUsd
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Get history price failed.");
                throw;
            }

            return new ListResultDto<TokenPriceDataDto>
            {
                Items = result
            };
        }
    }
    
    public class PriceDto
    {
        public decimal PriceInUsd { get; set; } = PriceOptions.DefaultPriceValue;
    }
}