using System;
using System.Threading.Tasks;
using AwakenServer.ExchangeClient;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Nethereum.Util;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.Price
{
    public interface IPriceProvider
    {
        public Task<BigDecimal> GetPriceAsync(string symbol);
    }
    
    public class PriceProvider : IPriceProvider, ISingletonDependency
    {
        private readonly IGateClient _gateClient;
        private readonly IAEXClient _aexClient;
        private readonly IBinanceClient _binanceClient;
        private readonly ISTARTokenOptions _starTokenOptions;
        private readonly IDistributedCache<string> _exchangePriceCache;
        private const string Prefix = "ExchangePrice";
        private const int CacheExpireMinutes = 1;

        public PriceProvider(IGateClient gateClient, IAEXClient aexClient,
            IOptionsSnapshot<ISTARTokenOptions> starTokenOptions,
            IDistributedCache<string> exchangePriceCache, IBinanceClient binanceClient)
        {
            _gateClient = gateClient;
            _aexClient = aexClient;
            _starTokenOptions = starTokenOptions.Value;
            _exchangePriceCache = exchangePriceCache;
            _binanceClient = binanceClient;
        }

        public async Task<BigDecimal> GetPriceAsync(string symbol)
        {
            if (Symbol.SASHIMI == symbol) return await GetSashimiPriceAsync();
            if (Symbol.BTC == symbol) return await GetBtcPriceAsync();
            if (Symbol.ISTAR == symbol) return await GetISTARPriceAsync();
            return 0;
        }

        private async Task<BigDecimal> GetBtcPriceAsync()
        {
            var key = $"{Prefix}-{Symbol.BTC}";
            var value = await _exchangePriceCache.GetAsync(key);
            if(value != null) return BigDecimal.Parse(value);
            var price = await GetBtcPriceFromExchangeAsync();
            await _exchangePriceCache.SetAsync(key, price.ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(CacheExpireMinutes)
            });
            return price;
        }

        private Task<BigDecimal> GetISTARPriceAsync()
        {
            return Task.FromResult(BigDecimal.Parse(_starTokenOptions.InitPrice));
        }

        private async Task<BigDecimal> GetSashimiPriceAsync()
        {
            var key = $"{Prefix}-{Symbol.SASHIMI}";
            var value = await _exchangePriceCache.GetAsync(key);
            if(value != null) return BigDecimal.Parse(value);

            var price = await GetSashimiPriceFromExchangeAsync();

            await _exchangePriceCache.SetAsync(key, price.ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(CacheExpireMinutes)
            });
            return price;
        }
        
        private async Task<BigDecimal> GetBtcPriceFromExchangeAsync()
        {
            var binancePrice = await _binanceClient.GetPriceAsync(_binanceClient.GetSymbol("BTC", "USDT"));
            var gatePrice= await _gateClient.GetPriceAsync(_gateClient.GetSymbol("BTC", "USDT"));
            var count = getCount(binancePrice, gatePrice);
            return count == 0 ? 0 : (gatePrice + binancePrice) / count;
        }
        
        private async Task<BigDecimal> GetSashimiPriceFromExchangeAsync()
        {
            var gatePrice = await _gateClient.GetPriceAsync(_gateClient.GetSymbol("SASHIMI", "USDT"));
            var aexPrice = await _aexClient.GetPriceAsync(_aexClient.GetSymbol("SASHIMI", "USDT"));
            var count = getCount(gatePrice, aexPrice);
            return count == 0 ? 0 : (gatePrice + aexPrice) / count;
        }

        private int getCount(BigDecimal price, BigDecimal otherPrice)
        {
            int count;
            if(price != 0 && otherPrice != 0)
            {
                count = 2;
            }
            else if (price == 0 && otherPrice == 0)
            {
                count = 0;
            }
            else
            {
                count = 1;
            }

            return count;
        }
    }
}