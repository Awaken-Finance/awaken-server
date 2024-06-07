using System;
using System.Globalization;
using System.Threading.Tasks;
using Aetherlink.PriceServer;
using Aetherlink.PriceServer.Dtos;
using AwakenServer.AetherLinkApi;
using AwakenServer.CoinGeckoApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;

namespace AwakenServer.Grains.Grain.Tokens.TokenPrice;

public class AetherLinkTokenPriceProvider : ITokenPriceProvider
{
    private readonly ILogger<AetherLinkTokenPriceProvider> _logger;
    private readonly IPriceServerProvider _priceServerProvider;
    private readonly IOptionsSnapshot<AetherLinkOptions> _aetherLinkOptions;
    
    public AetherLinkTokenPriceProvider(IPriceServerProvider priceServerProvider, 
        ILogger<AetherLinkTokenPriceProvider> logger,
        IOptionsSnapshot<AetherLinkOptions> options)
    {
        _priceServerProvider = priceServerProvider;
        _logger = logger;
        _aetherLinkOptions = options;
    }

    private string GetPriceTradePairAsync(string symbol)
    {
        var pricePair = _aetherLinkOptions.Value.CoinIdMapping.TryGetValue(symbol.ToUpper(), out var id) ? id : null;
        if (pricePair == null)
        {
            return null;
        }
        return pricePair;
    }
    
    public async Task<decimal> GetPriceAsync(string symbol)
    {
        if (string.IsNullOrEmpty(symbol))
        {
            return 0;
        }

        var priceTradePair = GetPriceTradePairAsync(symbol);
        if (priceTradePair == null)
        {
            _logger.Info("can not get the token {symbol}", symbol);
            return 0;
        }
        
        try
        {
            if (symbol == "SGR-1")
            {
                var result = (await _priceServerProvider.GetAggregatedTokenPriceAsync(new()
                {
                    TokenPair = "sgr-usdt",
                    AggregateType = AggregateType.Latest
                })).Data;

                _logger.LogInformation($"get token price from Aetherlink price service, {result.TokenPair}, {result.Price}, {result.Decimal}");
        
                var price = (decimal)(result.Price / Math.Pow(10, (double)result.Decimal));
                
                var usdtResult = (await _priceServerProvider.GetAggregatedTokenPriceAsync(new()
                {
                    TokenPair = "usdt-usd",
                    AggregateType = AggregateType.Latest
                })).Data;
                
                var usdtPrice = (decimal)(usdtResult.Price / Math.Pow(10, (double)usdtResult.Decimal));

                return price * usdtPrice;
            }
            else
            {
                var result = (await _priceServerProvider.GetAggregatedTokenPriceAsync(new()
                {
                    TokenPair = priceTradePair,
                    AggregateType = AggregateType.Latest
                })).Data;

                _logger.LogInformation($"get token price from Aetherlink price service, {result.TokenPair}, {result.Price}, {result.Decimal}");
        
                return (decimal)(result.Price / Math.Pow(10, (double)result.Decimal));
            }
            
        }
        catch (Exception e)
        {
            _logger.LogError($"get token price from Aetherlink price service faild, {symbol}, {e}");
            return 0;
        }
    }

    public async Task<decimal> GetHistoryPriceAsync(string symbol, string dateTime)
    {
        if (string.IsNullOrEmpty(symbol))
        {
            return 0;
        }

        var priceTradePair = GetPriceTradePairAsync(symbol);
        if (priceTradePair == null)
        {
            _logger.Info("can not get the token {symbol}", symbol);
            return 0;
        }
        
        var date = DateTime.ParseExact(dateTime, "dd-MM-yyyy", CultureInfo.InvariantCulture).ToString("yyyyMMdd");
        try
        {
            if (symbol == "SGR-1")
            {
                var tokenPair = "sgr-usdt";
                var result = (await _priceServerProvider.GetDailyPriceAsync(new()
                {
                    TokenPair = tokenPair,
                    TimeStamp = date
                })).Data;

                _logger.LogInformation($"get token daily price from Aetherlink price service, tokenPair: {tokenPair}, TimeStamp: {date}, result.Price: {result.Price}, result.Decimal: {result.Decimal}");
                
                var price = (decimal)(result.Price / Math.Pow(10, (double)result.Decimal));
                
                var usdtResult = (await _priceServerProvider.GetDailyPriceAsync(new()
                {
                    TokenPair = "usdt-usd",
                    TimeStamp = date
                })).Data;
                
                var usdtPrice = (decimal)(usdtResult.Price / Math.Pow(10, (double)usdtResult.Decimal));

                return price * usdtPrice;
            }
            else
            {
                var tokenPair = priceTradePair;
                var result = (await _priceServerProvider.GetDailyPriceAsync(new()
                {
                    TokenPair = tokenPair,
                    TimeStamp = date
                })).Data;

                _logger.LogInformation($"get token daily price from Aetherlink price service, tokenPair: {tokenPair}, TimeStamp: {date}, result.Price: {result.Price}, result.Decimal: {result.Decimal}");
        
                return (decimal)(result.Price / Math.Pow(10, (double)result.Decimal));
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"get token daily price from Aetherlink price service faild, {symbol}, {dateTime}, {e}");
            return 0;
        }
    }
    
}