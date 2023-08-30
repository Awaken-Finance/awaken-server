using System;

namespace AwakenServer.Models
{
    public class KLineModel<T>
    {
        public string ChainId { get; set; }

        public Guid TradePairId { get; set; }

        public int Period { get; set; }

        public long From { get; set; }

        public long To { get; set; }

        public T Data { get; set; }
    }
}