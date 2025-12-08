namespace AwakenServer.Trade;

public class TradePairConst
{
    public const int MaxPageSize = 200;
    
    public const int MaxIdsLength = 2048;
}

public enum TradePairPage
{
    TradePage = 1,
    MarketPage = 2
}

public enum TradePairFeature
{
    All = 0,
    Fav = 1,
    OtherSymbol = 2
}