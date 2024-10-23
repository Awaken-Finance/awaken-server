using AwakenServer.Trade;

namespace AwakenServer.Grains.Grain.Price.TradePair;

[GenerateSerializer]
public class TradePairMarketDataSnapshotGrainDto
{
    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public string ChainId { get; set; }
    [Id(2)]
    public Guid TradePairId { get; set; }
    [Id(3)]
    public string TotalSupply { get; set; } = "0";
    [Id(4)]
    public double Price { get; set; }
    [Id(5)]
    public double PriceUSD { get; set; }
    [Id(6)]
    public double PriceHigh { get; set; }
    [Id(7)]
    public double PriceHighUSD { get; set; }
    [Id(8)]
    public double PriceLow { get; set; }
    [Id(9)]
    public double PriceLowUSD { get; set; }
    [Id(10)]
    public double TVL { get; set; }
    [Id(11)]
    public double ValueLocked0 { get; set; }
    [Id(12)]
    public double ValueLocked1 { get; set; }
    [Id(13)]
    public double Volume { get; set; }
    [Id(14)]
    public double TradeValue { get; set; }
    [Id(15)]
    public int TradeCount { get; set; }
    [Id(16)]
    public int TradeAddressCount24h { get; set; }
    [Id(17)]
    public DateTime Timestamp { get; set; }
    [Id(18)]
    public string LpAmount { get; set; } = "0";
}

