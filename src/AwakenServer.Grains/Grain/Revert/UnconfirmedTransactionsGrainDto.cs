using System;
using AwakenServer.Common;
using AwakenServer.Trade;

namespace AwakenServer.Grains.Grain.Price.TradeRecord;

[GenerateSerializer]
public class UnconfirmedTransactionsGrainDto
{
    [Id(0)]
    public long BlockHeight { get; set; }
    [Id(1)]
    public string TransactionHash { get; set; }
}