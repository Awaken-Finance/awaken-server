using System;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AwakenServer.Price;
using CoinGecko.Clients;
using CoinGecko.Interfaces;
using Microsoft.Extensions.Options;
using Serilog;

namespace AwakenServer.CoinGeckoApi;

public class TokenPriceProvider : ITokenPriceProvider
{
    private readonly ICoinGeckoClient _coinGeckoClient;
    private readonly IRequestLimitProvider _requestLimitProvider;
    private readonly CoinGeckoOptions _coinGeckoOptions;
    private readonly ILogger _logger;
    private const int MaxRetryAttempts = 2;
    private const int DelayBetweenRetriesInSeconds = 3;
    
    public TokenPriceProvider(IRequestLimitProvider requestLimitProvider, IOptionsSnapshot<CoinGeckoOptions> options,
        IHttpClientFactory httpClientFactory)
    {
        _requestLimitProvider = requestLimitProvider;
        _coinGeckoClient = new CoinGeckoClient(httpClientFactory.CreateClient());
        _coinGeckoOptions = options.Value;
        _logger = Log.ForContext<TokenPriceProvider>();
    }

    [ExceptionHandler(typeof(Exception), Message = "GetPrice Error", TargetType = typeof(HandlerExceptionService), 
        MethodName = nameof(HandlerExceptionService.HandleWithReturn0))]
    public virtual async Task<decimal> GetPriceAsync(string symbol)
    {
        if (string.IsNullOrEmpty(symbol))
        {
            return 0;
        }

        var coinId = GetCoinIdAsync(symbol);
        if (coinId == null)
        {
            _logger.Information("can not get the token {symbol}", symbol);
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
            _logger.Information($"Get history token price {symbol}, can not get the token");
            return 0;
        }

        
        int retryAttempts = 0;
        
        while (retryAttempts < MaxRetryAttempts)
        {
            var coinData = await RequestAsync(async () =>
                await _coinGeckoClient.CoinsClient.GetHistoryByCoinId(coinId, dateTime, "false"));

            if (coinData == null || coinData.MarketData == null)
            {
                _logger.Error($"Get history token price {symbol}, Unexpected CoinGecko response: MarketData is null");
            }
            else
            {
                return (decimal) coinData.MarketData.CurrentPrice[CoinGeckoApiConsts.UsdSymbol].Value;
            }

            retryAttempts++;
            _logger.Warning($"Get history token price {symbol}, Attempt {retryAttempts} failed.");

            if (retryAttempts >= MaxRetryAttempts)
            {
                _logger.Error($"Get history token price {symbol}, Max retry attempts reached. Unable to get coin price.");
                return 0;
            }

            await Task.Delay(TimeSpan.FromSeconds(DelayBetweenRetriesInSeconds));
        }

        return 0;
    }

    private string GetCoinIdAsync(string symbol)
    {
        return _coinGeckoOptions.CoinIdMapping.TryGetValue(symbol.ToUpper(), out var id) ? id : null;
    }

    [ExceptionHandler(typeof(Exception),
        TargetType = typeof(HandlerExceptionService), MethodName = nameof(HandlerExceptionService.HandleWithReturnNull))]
    public virtual async Task<T> RequestAsync<T>(Func<Task<T>> task)
    {
        await _requestLimitProvider.RecordRequestAsync();
        return await task();
    }
}