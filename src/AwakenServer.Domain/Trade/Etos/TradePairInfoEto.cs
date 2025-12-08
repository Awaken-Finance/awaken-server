using System;
using AutoMapper;

namespace AwakenServer.Trade.Etos
{
    [AutoMap(typeof(TradePair))]
    public class TradePairInfoEto : TradePairInfoIndex
    {
        public TradePairInfoEto()
        {
        }

        public TradePairInfoEto(Guid id)
        {
            Id = id;
        }
    }
}