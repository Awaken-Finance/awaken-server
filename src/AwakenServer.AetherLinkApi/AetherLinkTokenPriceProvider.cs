using System;
using System.Globalization;
using System.Threading.Tasks;
using Aetherlink.PriceServer;
using Aetherlink.PriceServer.Dtos;
using AwakenServer.AetherLinkApi;
using AwakenServer.Price;
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

    
    
    public async Task<decimal> GetPriceAsync(string pair)
    {
        try
        {
            var result = (await _priceServerProvider.GetAggregatedTokenPriceAsync(new()
            {
                TokenPair = pair,
                AggregateType = AggregateType.Latest
            })).Data;

            _logger.LogInformation($"Get token price from Aetherlink price service, pair: {result.TokenPair}, price: {result.Price}, decimal: {result.Decimal}");
    
            return (decimal)(result.Price / Math.Pow(10, (double)result.Decimal));
        }
        catch (Exception e)
        {
            _logger.LogError($"Get token price from Aetherlink price service faild, pair: {pair}, exception: {e}");
            throw;
        }
    }

    public async Task<decimal> GetHistoryPriceAsync(string pair, string dateTime)
    {
        var date = DateTime.ParseExact(dateTime, "dd-MM-yyyy", CultureInfo.InvariantCulture).ToString("yyyyMMdd");
        try
        {
            var tokenPair = pair;
            var result = (await _priceServerProvider.GetDailyPriceAsync(new()
            {
                TokenPair = tokenPair,
                TimeStamp = date
            })).Data;

            _logger.LogInformation($"Get history token price from Aetherlink price service, tokenPair: {tokenPair}, TimeStamp: {date}, result.Price: {result.Price}, result.Decimal: {result.Decimal}");
    
            return (decimal)(result.Price / Math.Pow(10, (double)result.Decimal));
        }
        catch (Exception e)
        {
            _logger.LogError($"Get history token price from Aetherlink price service faild, {pair}, {dateTime}, {e}");
            return 0;
        }
    }
    
}