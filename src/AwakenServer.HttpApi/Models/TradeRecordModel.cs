using System;

namespace AwakenServer.Models
{
    public class TradeRecordModel<T>
    {
        public string ChainId { get; set; }

        public Guid TradePairId { get; set; }

        public T Data { get; set; }
    }
}