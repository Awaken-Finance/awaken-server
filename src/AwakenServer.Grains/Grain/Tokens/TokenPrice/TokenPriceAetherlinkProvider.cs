using System;
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
            var result = (await _priceServerProvider.GetTokenPriceAsync(new()
            {
                TokenPair = $"{symbol.ToLower()}-usd",
                Source = SourceType.CoinGecko
            })).Data;

            _logger.LogInformation($"get token price from Aetherlink price service, {result.TokenPair}, {result.Price}, {result.Decimal}");
        
            return (decimal)(result.Price / Math.Pow(10, (double)result.Decimal));
        }
        catch (Exception e)
        {
            _logger.LogError($"get token price from Aetherlink price service faild, {symbol}, {e}");
            return 0;
        }
    }

    public async Task<decimal> GetHistoryPriceAsync(string symbol, string dateTime)
    {
        try
        {
            var result = (await _priceServerProvider.GetTokenPriceAsync(new()
            {
                TokenPair = $"{symbol.ToLower()}-usd",
                Source = SourceType.CoinGecko
            })).Data;

            _logger.LogInformation($"get token price from Aetherlink price service, {result.TokenPair}, {result.Price}, {result.Decimal}");
        
            return (decimal)(result.Price / Math.Pow(10, (double)result.Decimal));
        }
        catch (Exception e)
        {
            _logger.LogError($"get token price from Aetherlink price service faild, {symbol}, {e}");
            return 0;
        }
    }
    
}