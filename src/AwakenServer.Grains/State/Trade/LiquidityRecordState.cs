using System;
using System.Collections.Generic;
using AwakenServer.Trade;

namespace AwakenServer.Grains.State.Trade;

[GenerateSerializer]
public class LiquidityRecordState
{
    [Id(0)]
    public bool IsDeleted { get; set; }
    [Id(1)]
    public string TransactionHash { get; set; }
    [Id(2)]
    public string ChainId { get; set; }
    [Id(3)]
    public string PairAddress { get; set; }
    [Id(4)]
    public string LpTokenAmount { get; set; }
    [Id(5)]
    public DateTime Timestamp { get; set; }
    [Id(6)]
    public LiquidityType Type { get; set; }
    [Id(7)]
    public long BlockHeight { get; set; }
}