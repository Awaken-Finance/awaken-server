using System.Collections.Generic;

namespace AwakenServer.Price;

public class PriceOptions
{
    public const string PriceCachePrefix = "Price";
    public const string PriceHistoryCachePrefix = "PriceHistory";
    // from 60 * 60 * 24 * 365 * 50
    public const int PriceSuperLongExpirationTime = 1576800000;
    public const decimal DefaultPriceValue = -1;
    public const string UsdtPricePair = "usdt-usd";
}

public class TokenPriceOptions
{
    public int PriceExpirationTimeSeconds = 3600;
    public List<string> UsdtPriceTokens { set; get; } 
    public Dictionary<string, string> PriceTokenMapping { get; set; }
}