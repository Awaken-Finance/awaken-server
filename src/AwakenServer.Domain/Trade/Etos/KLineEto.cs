using System;
using AutoMapper;

namespace AwakenServer.Trade.Etos
{
    [AutoMap(typeof(KLine))]
    public class KLineEto : KLine
    {
        public KLineEto()
        {
        }

        public KLineEto(Guid id)
            : base(id)
        {
        }
    }
}