using System;
using AwakenServer.Entities;
using Nest;

namespace AwakenServer.Trade;

public class CurrentUserLiquidity : MultiChainEntity<Guid>
{
    [Keyword] public Guid TradePairId { get; set; }
    [Keyword] public string Address { get; set; }
    public long LpTokenAmount { get; set; }
    public DateTime LastUpdateTime { get; set; }
    public long Token0CumulativeAddition { get; set; }
    public long Token1CumulativeAddition { get; set; }
    public long AverageHoldingStartTime { get; set; }
    public long Token0UnReceivedFee { get; set; }
    public long Token1UnReceivedFee { get; set; }
    public long Token0ReceivedFee { get; set; }
    public long Token1ReceivedFee { get; set; }
}