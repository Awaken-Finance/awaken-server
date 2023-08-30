using System;

namespace AwakenServer.Trade.Dtos
{
    public class LiquidityUpdateDto
    {
        public string ChainId { get; set; }
        public Guid TradePairId { get; set; }
        public string Token0Amount { get; set; }
        public string Token1Amount { get; set; }
        public long Timestamp { get; set; }
    }
}