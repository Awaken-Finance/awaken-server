using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Aetherlink.PriceServer;
using Aetherlink.PriceServer.Dtos;
using AwakenServer.Grains.Grain.Tokens.TokenPrice;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.Grains.Grain.Tokens.TokenPrice;

public class TokenPriceAetherlinkProvider : ITokenPriceProvider
{
    private readonly ILogger<TokenPriceAetherlinkProvider> _logger;
    private readonly IPriceServerProvider _priceServerProvider;
    
    public TokenPriceAetherlinkProvider(IPriceServerProvider priceServerProvider, 
        ILogger<TokenPriceAetherlinkProvider> logger)
    {
        _priceServerProvider = priceServerProvider;
        _logger = logger;
    }

    public async Task<decimal> GetPriceAsync(string symbol)
    {
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
                    TokenPair = $"{symbol.ToLower()}-usd",
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
                var tokenPair = $"{symbol.ToLower()}-usd";
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