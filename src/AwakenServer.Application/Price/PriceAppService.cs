using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwakenServer.Grains.Grain.Tokens.TokenPrice;
using AwakenServer.Price.Dtos;
using AwakenServer.Tokens.Dtos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly IOptionsSnapshot<TokenPriceOptions> _tokenPriceOptions;
        
        public PriceAppService(IDistributedCache<PriceDto> priceCache,
            ITokenPriceProvider tokenPriceProvider,
            IOptionsSnapshot<TokenPriceOptions> options)
        {
            _priceCache = priceCache;
            _tokenPriceProvider = tokenPriceProvider;
            _tokenPriceOptions = options;
        }

        public async Task<string> GetTokenPriceAsync(GetTokenPriceInput input)
        {
            if (string.IsNullOrWhiteSpace(input.Symbol)) return "0";
            var result = await GetTokenPriceListAsync(new List<string>{ input.Symbol });
            if (result.Items.Count == 0) return "0";
            else return result.Items[0].PriceInUsd.ToString();
        }

        private async Task<decimal> GetUsdtPriceAsync(string time)
        {
            if (String.IsNullOrEmpty(time))
            {
                return await _tokenPriceProvider.GetPriceAsync(PriceOptions.UsdtPricePair);
            }
           
            return await _tokenPriceProvider.GetHistoryPriceAsync(PriceOptions.UsdtPricePair, time);
        }

        private string GetPriceTradePair(string symbol)
        {
            if (String.IsNullOrEmpty(symbol))
            {
                return null;
            }

            _tokenPriceOptions.Value.PriceTokenMapping.TryGetValue(symbol.ToUpper(), out var priceTradePair);
            
            return priceTradePair;
        }
        
        private async Task<decimal> ProcessTokenPrice(string symbol, decimal rawPrice, string time)
        {
            if (_tokenPriceOptions.Value.UsdtPriceTokens.Contains(symbol))
            {
                var usdtPrice = await GetUsdtPriceAsync(time);
                return rawPrice * usdtPrice;
            }

            return rawPrice;
        }
        
        
        private async Task<decimal> GetPriceAsync(string symbol)
        {
            var pair = GetPriceTradePair(symbol);
            if (String.IsNullOrEmpty(pair))
            {
                Logger.LogInformation($"Get price, symbol: {symbol}, result price: 0");
                return 0;
            }
            
            var rawPrice = await _tokenPriceProvider.GetPriceAsync(pair);
            var result = await ProcessTokenPrice(symbol, rawPrice, null);
            
            Logger.LogInformation($"Get price, symbol: {symbol}, pair: {pair}, rawPrice: {rawPrice}, result price: {result}");
            
            return result;
        }

        private async Task<decimal> GetHistoryPriceAsync(string symbol, string time)
        {
            var pair = GetPriceTradePair(symbol);
            if (String.IsNullOrEmpty(pair))
            {
                Logger.LogInformation($"Get price, symbol: {symbol}, result price: 0");
                return 0;
            }
            
            var rawPrice = await _tokenPriceProvider.GetHistoryPriceAsync(pair, time);
            var result = await ProcessTokenPrice(symbol, rawPrice, time);
            
            Logger.LogInformation($"Get price, symbol: {symbol}, pair: {pair}, time: {time}, rawPrice: {rawPrice}, result price: {result}");
            
            return result;
        }

        private bool IsNeedFetchPrice(PriceDto priceDto)
        {
            return priceDto.PriceInUsd == PriceOptions.DefaultPriceValue ||
                   priceDto.PriceUpdateTime.AddSeconds(_tokenPriceOptions.Value.PriceExpirationTimeSeconds) <= DateTime.UtcNow;
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
                    var price = await _priceCache.GetOrAddAsync(key, async () => new PriceDto());
                    
                    if (IsNeedFetchPrice(price))
                    {
                        try
                        {
                            price.PriceInUsd = await GetPriceAsync(symbolList[i]);
                            price.PriceUpdateTime = DateTime.UtcNow;
                            await _priceCache.SetAsync(key, price);
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(e, $"Get price symbol: {symbolList[i]} failed. Return old data price: {price.PriceInUsd}");
                        }
                    }
                    
                    Logger.LogInformation("Get price, {symbol}, {priceInUsd}", symbolList[i], price.PriceInUsd);
                    
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
                    var price = await _priceCache.GetOrAddAsync(key, async () => new PriceDto());
                    
                    if (IsNeedFetchPrice(price))
                    {
                        try
                        {
                            price.PriceInUsd = await GetHistoryPriceAsync(input.Symbol, time);
                            price.PriceUpdateTime = DateTime.UtcNow;
                            await _priceCache.SetAsync(key, price);
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(e, $"Get history price symbol: {input.Symbol}, time: {time} failed. Return old data price: {price.PriceInUsd}");
                        }
                       
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
        public DateTime PriceUpdateTime { get; set; }
        
    }
}