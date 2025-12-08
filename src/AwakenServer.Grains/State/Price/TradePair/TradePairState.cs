using System;
using System.Collections.Generic;
using AwakenServer.Tokens;
using AwakenServer.Trade;

namespace AwakenServer.Grains.State.Trade;

[GenerateSerializer]
public class TradePairState
{
    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public bool IsDeleted { get; set; }
    [Id(2)]
    public HashSet<string> MarketDataSnapshotGrainIds { get; set; } = new();
    [Id(3)]
    public virtual string ChainId { get; set; }
    [Id(4)]
    public Token Token0 { get; set; }
    [Id(5)]
    public Token Token1 { get; set; }
    [Id(6)]
    public string Address { get; set; }
    [Id(7)]
    public double FeeRate { get; set; }
    [Id(8)]
    public bool IsTokenReversed { get; set; }
    [Id(9)]
    public string TotalSupply { get; set; }
    [Id(10)]
    public double Price { get; set; }
    [Id(11)]
    public double PriceUSD { get; set; }
    [Id(12)]
    public double PricePercentChange24h { get; set; }
    [Id(13)]
    public double PriceChange24h { get; set; }
    [Id(14)]
    public double PriceHigh24h { get; set; }
    [Id(15)]
    public double PriceLow24h { get; set; }
    [Id(16)]
    public double PriceHigh24hUSD { get; set; }
    [Id(17)]
    public double PriceLow24hUSD { get; set; }
    [Id(18)]
    public double Volume24h { get; set; }
    [Id(19)]
    public double VolumePercentChange24h { get; set; }
    [Id(20)]
    public double TradeValue24h { get; set; }
    [Id(21)]
    public double TVL { get; set; }
    [Id(22)]
    public double TVLPercentChange24h { get; set; }
    [Id(23)]
    public double ValueLocked0 { get; set; }
    [Id(24)]
    public double ValueLocked1 { get; set; }
    [Id(25)]
    public int TradeCount24h { get; set; }
    [Id(26)]
    public int TradeAddressCount24h { get; set; }
    [Id(27)]
    public double FeePercent7d { get; set; }
}