namespace AwakenServer.CoinGeckoApi;

public class CoinGeckoApiConsts
{
    public const string RequestTimeCacheKey = "RequestTime";
    public const string UsdSymbol = "usd";
    // The CoinGecko limit 10-30 requests/minute;
    public const int MaxRequestTime = 100;
}