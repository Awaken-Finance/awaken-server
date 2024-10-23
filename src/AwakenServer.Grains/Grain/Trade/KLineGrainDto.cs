using AwakenServer.Trade;

namespace AwakenServer.Grains.Grain.Trade;

[GenerateSerializer]
public class KLineGrainDto
{
    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public string ChainId { get; set; }
    [Id(2)]
    public Guid TradePairId { get; set; }
    [Id(3)]
    public int Period { get; set; }
    [Id(4)]
    public double Open { get; set; }
    [Id(5)]
    public double Close { get; set; }
    [Id(6)]
    public double High { get; set; }
    [Id(7)]
    public double Low { get; set; }
    [Id(8)]
    public double OpenWithoutFee { get; set; }
    [Id(9)]
    public double CloseWithoutFee { get; set; }
    [Id(10)]
    public double HighWithoutFee { get; set; }
    [Id(11)]
    public double LowWithoutFee { get; set; }
    [Id(12)]
    public double Volume { get; set; }
    [Id(13)]
    public long Timestamp { get; set; }
    [Id(14)]
    public string GrainId { get; set; }
}