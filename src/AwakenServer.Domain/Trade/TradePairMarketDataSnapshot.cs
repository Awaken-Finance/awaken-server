using System;

namespace AwakenServer.Trade
{
    public class TradePairMarketDataSnapshot : TradePairMarketDataBase
    {
        public TradePairMarketDataSnapshot()
        {
        }

        public TradePairMarketDataSnapshot(Guid id)
            : base(id)
        {
        }
    }
}