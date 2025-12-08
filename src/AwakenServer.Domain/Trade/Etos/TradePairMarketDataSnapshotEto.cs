using System;
using AutoMapper;

namespace AwakenServer.Trade.Etos
{
    [AutoMap(typeof(TradePairMarketDataSnapshot))]
    public class TradePairMarketDataSnapshotEto : TradePairMarketDataSnapshot
    {
        public TradePairMarketDataSnapshotEto()
        {
        }

        public TradePairMarketDataSnapshotEto(Guid id)
            : base(id)
        {
            Id = id;
        }
    }
}