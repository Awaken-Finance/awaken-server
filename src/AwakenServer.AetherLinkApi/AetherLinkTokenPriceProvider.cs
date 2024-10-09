using System;
using System.Globalization;
using System.Threading.Tasks;
using Aetherlink.PriceServer;
using Aetherlink.PriceServer.Dtos;
using AwakenServer.AetherLinkApi;
using AwakenServer.Price;
using Microsoft.Extensions.Options;
using Serilog;

namespace AwakenServer.Grains.Grain.Tokens.TokenPrice;

public class AetherLinkTokenPriceProvider : ITokenPriceProvider
{
    private readonly IPriceServerProvider _priceServerProvider;
    private readonly IOptionsSnapshot<AetherLinkOptions> _aetherLinkOptions;
    
    public AetherLinkTokenPriceProvider(IPriceServerProvider priceServerProvider,
        IOptionsSnapshot<AetherLinkOptions> options)
    {
        _priceServerProvider = priceServerProvider;
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

            Log.Information($"Get token price from Aetherlink price service, pair: {result.TokenPair}, price: {result.Price}, decimal: {result.Decimal}");
    
            return (decimal)(result.Price / Math.Pow(10, (double)result.Decimal));
        }
        catch (Exception e)
        {
            Log.Error($"Get token price from Aetherlink price service faild, pair: {pair}, exception: {e}");
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

            Log.Information($"Get history token price from Aetherlink price service, tokenPair: {tokenPair}, TimeStamp: {date}, result.Price: {result.Price}, result.Decimal: {result.Decimal}");
    
            return (decimal)(result.Price / Math.Pow(10, (double)result.Decimal));
        }
        catch (Exception e)
        {
            Log.Error(e, $"Get history token price from Aetherlink price service faild, {pair}, {dateTime}, {e.Message}");
            return 0;
        }
    }
    
}