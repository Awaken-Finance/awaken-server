using System;
using AutoMapper;

namespace AwakenServer.Trade.Etos
{
    [AutoMap(typeof(LiquidityRecord))]
    public class LiquidityRecordEto : LiquidityRecord
    {
        public LiquidityRecordEto()
        {
        }

        public LiquidityRecordEto(Guid id)
            : base(id)
        {
            Id = id;
        }
    }
}