using System;

namespace AwakenServer.GameOfTrust.DTos
{
    public class MarketDataDto 
    {   
        public Guid Id { get; set; }
        public long Timestamp { get; set; }
        public string MarketCap { get; set; }
        public string Price { get; set; }
        public string TotalSupply { get; set; }
    }
}