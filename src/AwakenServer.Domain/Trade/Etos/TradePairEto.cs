using System;
using AutoMapper;

namespace AwakenServer.Trade.Etos
{
    [AutoMap(typeof(TradePair))]
    public class TradePairEto : TradePair
    {
        public TradePairEto()
        {
        }

        public TradePairEto(Guid id)
            : base(id)
        {
            Id = id;
        }
    }
}