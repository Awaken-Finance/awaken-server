using AwakenServer.Trade;

namespace AwakenServer.Grains.Grain.Price.TradePair;

public class TradePairMarketDataSnapshotGrainDto : TradePairMarketDataBase
{
    public string LpAmount { get; set; } = "0";
}

