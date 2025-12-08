using System;

namespace AwakenServer.Trade.Dtos;

public class TradePairMarketDataSnapshotDto
{
    
    public Guid Id { get; set; }
    public string ChainId { get; set; }
    public Guid TradePairId { get; set; }
    
    public string TotalSupply { get; set; } = "0";
    
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
    
    public string LpAmount { get; set; } = "0";
}