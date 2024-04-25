namespace AwakenServer.Common;

public enum WorkerBusinessType
{
    LiquidityEvent,
    SwapEvent,
    SyncEvent,
    TradePairEvent,
    TradeRecordRevert, 
    TradePairUpdate,
    MarketSnapshot,
    TransactionRevert
}