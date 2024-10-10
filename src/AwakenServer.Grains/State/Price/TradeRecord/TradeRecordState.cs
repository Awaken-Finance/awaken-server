using System;
using AwakenServer.Trade;

namespace AwakenServer.Grains.State.Price;

[GenerateSerializer]
public class TradeRecordState
{
    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public string ChainId { get; set; }
    [Id(2)]
    public Guid TradePairId { get; set; }
    [Id(3)]
    public string Address { get; set; }
    [Id(4)]
    public TradeSide Side { get; set; } = ((TradeSide[])Enum.GetValues(typeof(TradeSide)))[0];
    [Id(5)]
    public string Token0Amount { get; set; }
    [Id(6)]
    public string Token1Amount { get; set; }
    [Id(7)]
    public DateTime Timestamp { get; set; }
    [Id(8)]
    public string TransactionHash { get; set; }
    [Id(9)]
    public string Channel { get; set; }
    [Id(10)]
    public string Sender { get; set; }
    [Id(11)]
    public double Price { get; set; }
}