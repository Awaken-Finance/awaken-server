using System;
using AutoMapper;

namespace AwakenServer.Trade.Etos
{
    [AutoMap(typeof(TradeRecord))]
    public class TradeRecordEto : TradeRecord
    {
        public TradeRecordEto()
        {
        }

        public TradeRecordEto(Guid id)
            : base(id)
        {            
            Id = id;
        }
    }
}