using System;
using AwakenServer.Entities;
using AwakenServer.Trade;
using AwakenServer.Trade.Index;
using Nest;

namespace AwakenServer.StatInfo;

public class TransactionHistory : MultiChainEntity<Guid>
{
    [Keyword] public string Version { get; set; }
    public long Timestamp { get; set; }
    public TradePairWithToken TradePair { get; set; }
    public TransactionType TransactionType { get; set; }
    public double ValueInUsd { get; set; }
    [Keyword] public string Token0Amount { get; set; }
    [Keyword] public string Token1Amount { get; set; }
    public TradeSide Side { get; set; }
    public string TransactionHash { get; set; }
}
