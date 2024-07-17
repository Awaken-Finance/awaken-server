using System;
using AwakenServer.Entities;
using Nest;

namespace AwakenServer.Trade;

public class UserLiquiditySnapshot : MultiChainEntity<Guid>
{
    [Keyword] public Guid TradePairId { get; set; }
    [Keyword] public string Address { get; set; }
    [Keyword] public string Version { get; set; }
    public long LpTokenAmount { get; set; }
    public DateTime SnapShotTime { get; set; }
    public long Token0TotalFee { get; set; }
    public long Token1TotalFee { get; set; }
}