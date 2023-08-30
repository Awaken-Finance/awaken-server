namespace AwakenServer.Price;

public class PriceOptions
{
    public const string PriceCachePrefix = "Price";
    public const string PriceHistoryCachePrefix = "PriceHistory";
    public const int PriceExpirationTime = 3600;
    // from 60 * 60 * 24 * 365 * 50
    public const int PriceSuperLongExpirationTime = 1576800000;
    public const decimal DefaultPriceValue = -1;
}