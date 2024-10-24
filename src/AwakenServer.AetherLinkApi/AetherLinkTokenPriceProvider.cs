using System;
using System.Globalization;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
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
    private readonly ILogger _logger;
    
    public AetherLinkTokenPriceProvider(IPriceServerProvider priceServerProvider,
        IOptionsSnapshot<AetherLinkOptions> options)
    {
        _priceServerProvider = priceServerProvider;
        _aetherLinkOptions = options;
        _logger = Log.ForContext<AetherLinkTokenPriceProvider>();
    }

    
    
    [ExceptionHandler(typeof(Exception), Message = "GetPrice Error", LogOnly = true)]
    public virtual async Task<decimal> GetPriceAsync(string pair)
    {
        var result = (await _priceServerProvider.GetAggregatedTokenPriceAsync(new()
        {
            TokenPair = pair,
            AggregateType = AggregateType.Latest
        })).Data;

        _logger.Information(
            $"Get token price from Aetherlink price service, pair: {result.TokenPair}, price: {result.Price}, decimal: {result.Decimal}");

        return (decimal) (result.Price / Math.Pow(10, (double) result.Decimal));
    }

    [ExceptionHandler(typeof(Exception), Message = "GetHistoryPrice Error", ReturnDefault = ReturnDefault.Default)]
    public virtual async Task<decimal> GetHistoryPriceAsync(string pair, string dateTime)
    {
        var date = DateTime.ParseExact(dateTime, "dd-MM-yyyy", CultureInfo.InvariantCulture).ToString("yyyyMMdd");

        var tokenPair = pair;
        var result = (await _priceServerProvider.GetDailyPriceAsync(new()
        {
            TokenPair = tokenPair,
            TimeStamp = date
        })).Data;

        _logger.Information(
            $"Get history token price from Aetherlink price service, tokenPair: {tokenPair}, TimeStamp: {date}, result.Price: {result.Price}, result.Decimal: {result.Decimal}");

        return (decimal) (result.Price / Math.Pow(10, (double) result.Decimal));
    }
    
}