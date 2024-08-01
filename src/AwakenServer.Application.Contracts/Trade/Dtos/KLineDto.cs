using System;

namespace AwakenServer.Trade.Dtos
{
    public class KLineDto
    {
        public string ChainId { get; set; }
        public Guid TradePairId { get; set; }
        public int Period { get; set; }
        public double Open { get; set; }
        public double Close { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double OpenWithoutFee { get; set; }
        public double CloseWithoutFee { get; set; }
        public double HighWithoutFee { get; set; }
        public double LowWithoutFee { get; set; }
        public double Volume { get; set; }
        public long Timestamp { get; set; }
    }
}