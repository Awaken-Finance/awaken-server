using System;
using AwakenServer.Entities;
using Nest;

namespace AwakenServer.Trade
{
    public abstract class TradePairMarketDataBase : MultiChainEntity<Guid>
    {
        [Keyword] public Guid TradePairId { get; set; }
        [Keyword] public string TotalSupply { get; set; } = "0";
        public double Price { get; set; }
        public double PriceUSD { get; set; }
        public double PriceHigh { get; set; }
        public double PriceHighUSD { get; set; }
        public double PriceLow { get; set; }
        public double PriceLowUSD { get; set; }
        public double TVL { get; set; }
        public double ValueLocked0 { get; set; }
        public double ValueLocked1 { get; set; }
        public double Volume { get; set; }
        public double TradeValue { get; set; }
        public int TradeCount { get; set; }
        public int TradeAddressCount24h { get; set; }
        public DateTime Timestamp { get; set; }
        
        protected TradePairMarketDataBase()
        {
        }

        protected TradePairMarketDataBase(Guid id)
            : base(id)
        {
        }
    }
}