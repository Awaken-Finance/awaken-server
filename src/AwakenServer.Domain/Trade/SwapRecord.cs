using System;
using Nest;

namespace AwakenServer.Trade;

public class SwapRecord
{
    public string PairAddress { get; set; }
    public Guid TradePairId { get; set; }
    public long AmountOut { get; set; }
    public long AmountIn { get; set; }
    public long TotalFee { get; set; }
    [Keyword] public string SymbolOut { get; set; }
    [Keyword] public string SymbolIn { get; set; }
    [Keyword] public string Channel { get; set; }
}