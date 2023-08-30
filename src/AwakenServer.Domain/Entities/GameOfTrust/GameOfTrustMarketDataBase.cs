using System;

namespace AwakenServer.Entities.GameOfTrust
{
    public class GameOfTrustMarketDataBase : MultiChainEntity<Guid>
    {
        public string MarketCap { get; set; }
        public string Price { get; set; }
        public string TotalSupply { get; set; }
        public DateTime Timestamp { get; set; }
    }
}