using System;
using System.Collections.Generic;
using AwakenServer.Common;
using AwakenServer.Trade;

namespace AwakenServer.Grains.State.Price;

[GenerateSerializer]
public class UnconfirmedTransactionsState
{
    [Id(0)]
    public long MinUnconfirmedBlockHeight { get; set; }
    [Id(1)]
    public Dictionary<long, List<ToBeConfirmRecord>> UnconfirmedTransactions { get; set; }  = new();
}

[GenerateSerializer]
public class ToBeConfirmRecord
{
    [Id(0)]
    public string TransactionHash { get; set; }
}