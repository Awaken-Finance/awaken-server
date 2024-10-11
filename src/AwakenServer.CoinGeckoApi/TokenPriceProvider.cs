using System;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AwakenServer.Grains.Grain.Tokens.TokenPrice;
using AwakenServer.Price;
using CoinGecko.Clients;
using CoinGecko.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.CoinGeckoApi;

public class TokenPriceProvider : ITokenPriceProvider
{
    private readonly ICoinGeckoClient _coinGeckoClient;
    private readonly IRequestLimitProvider _requestLimitProvider;
    private readonly CoinGeckoOptions _coinGeckoOptions;
    private readonly ILogger<TokenPriceProvider> _logger;
    private const int MaxRetryAttempts = 2;
    private const int DelayBetweenRetriesInSeconds = 3;
    
    public TokenPriceProvider(IRequestLimitProvider requestLimitProvider, IOptionsSnapshot<CoinGeckoOptions> options,
        IHttpClientFactory httpClientFactory, ILogger<TokenPriceProvider> logger)
    {
        _requestLimitProvider = requestLimitProvider;
        _coinGeckoClient = new CoinGeckoClient(httpClientFactory.CreateClient());
        _coinGeckoOptions = options.Value;
        _logger = logger;
    }

    [ExceptionHandler(typeof(Exception), Message = "GetPrice Error", LogOnly = true)]
    public virtual async Task<decimal> GetPriceAsync(string symbol)
    {
        if (string.IsNullOrEmpty(symbol))
        {
            return 0;
        }

        var coinId = GetCoinIdAsync(symbol);
        if (coinId == null)
        {
            _logger.LogInformation("can not get the token {symbol}", symbol);
            return 0;
        }

        var coinData =
            await RequestAsync(async () =>
                await _coinGeckoClient.SimpleClient.GetSimplePrice(new[] {coinId},
                    new[] {CoinGeckoApiConsts.UsdSymbol}));

        if (!coinData.TryGetValue(coinId, out var value))
        {
            return 0;
        }

        return value[CoinGeckoApiConsts.UsdSymbol].Value;
    }

    public async Task<decimal> GetHistoryPriceAsync(string symbol, string dateTime)
    {
        if (string.IsNullOrEmpty(symbol))
        {
            return 0;
        }

        var coinId = GetCoinIdAsync(symbol);
        if (coinId == null)
        {
            _logger.LogInformation($"Get history token price {symbol}, can not get the token");
            return 0;
        }

        
        int retryAttempts = 0;
        
        while (retryAttempts < MaxRetryAttempts)
        {
            try
            {
                var coinData = await RequestAsync(async () => await _coinGeckoClient.CoinsClient.GetHistoryByCoinId(coinId, dateTime, "false"));

                if (coinData.MarketData == null)
                {
                    throw new Exception($"Get history token price {symbol}, Unexpected CoinGecko response: MarketData is null");
                }

                return (decimal)coinData.MarketData.CurrentPrice[CoinGeckoApiConsts.UsdSymbol].Value;
            }
            catch (Exception ex)
            {
                retryAttempts++;
                Log.Warning($"Get history token price {symbol}, Attempt {retryAttempts} failed: {ex.Message}");

                if (retryAttempts >= MaxRetryAttempts)
                {
                    Log.Error($"Get history token price {symbol}, Max retry attempts reached. Unable to get coin price.");
                    return 0;
                }

                await Task.Delay(TimeSpan.FromSeconds(DelayBetweenRetriesInSeconds));
            }
        }

        return 0;
    }

    private string GetCoinIdAsync(string symbol)
    {
        return _coinGeckoOptions.CoinIdMapping.TryGetValue(symbol.ToUpper(), out var id) ? id : null;
    }

    private async Task<T> RequestAsync<T>(Func<Task<T>> task)
    {
        await _requestLimitProvider.RecordRequestAsync();
        return await task();
    }
}