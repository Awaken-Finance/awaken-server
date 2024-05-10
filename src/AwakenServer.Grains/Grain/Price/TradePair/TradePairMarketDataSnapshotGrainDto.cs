using AwakenServer.Trade;
using Nethereum.Util;

namespace AwakenServer.Grains.Grain.Price.TradePair;

public class TradePairMarketDataSnapshotGrainDto : TradePairMarketDataBase
{
    public BigDecimal LpTokenAmount { get; set; }
}

