using System;
using System.Collections.Generic;

namespace AwakenServer.Trade.Dtos
{
    public class TradeRecordRemovedListResultDto
    {
        public List<TradeRecordRemovedDto> Items { get; set; }
    }

    public class TradeRecordRemovedDto
    {
        public string ChainId { get; set; }
        public Guid TradePairId { get; set; }
        public string Address { get; set; }
        public string TransactionHash { get; set; }
    }
    
    public class ReceiveTradeRecordRemovedDto
    {
        public string TransactionHash { get; set; }
    }
}