using System;
using System.Collections.Generic;
using AwakenServer.Tokens;
using AwakenServer.Trade;

namespace AwakenServer.Grains.State.Trade;

public class TradePairState
{
    public Guid Id { get; set; }
    public bool IsDeleted { get; set; }
    public HashSet<string> MarketDataSnapshotGrainIds { get; set; } = new();
    public virtual string ChainId { get; set; }
    public Token Token0 { get; set; }
    public Token Token1 { get; set; }
    
    public string Address { get; set; }
    public double FeeRate { get; set; }
    public bool IsTokenReversed { get; set; }
    public string TotalSupply { get; set; }
    public double Price { get; set; }
    public double PriceUSD { get; set; }
    public double PricePercentChange24h { get; set; }
    public double PriceChange24h { get; set; }
    public double PriceHigh24h { get; set; }
    public double PriceLow24h { get; set; }
    public double PriceHigh24hUSD { get; set; }
    public double PriceLow24hUSD { get; set; }
    public double Volume24h { get; set; }
    public double VolumePercentChange24h { get; set; }
    public double TradeValue24h { get; set; }
    public double TVL { get; set; }
    public double TVLPercentChange24h { get; set; }
    public double ValueLocked0 { get; set; }
    public double ValueLocked1 { get; set; }
    public int TradeCount24h { get; set; }
    public int TradeAddressCount24h { get; set; }
    public double FeePercent7d { get; set; }
}