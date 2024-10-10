using System;

namespace AwakenServer.Grains.Grain.Trade;

[GenerateSerializer]
public class UserTradeSummaryGrainDto
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
    public DateTime LatestTradeTime { get; set; }
}