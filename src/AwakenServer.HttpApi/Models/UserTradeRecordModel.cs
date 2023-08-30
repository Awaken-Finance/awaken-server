using System;

namespace AwakenServer.Models
{
    public class UserTradeRecordModel<T>
    {
        public string ChainId { get; set; }

        public Guid TradePairId { get; set; }

        public string Address { get; set; }

        public T Data { get; set; }
    }
}