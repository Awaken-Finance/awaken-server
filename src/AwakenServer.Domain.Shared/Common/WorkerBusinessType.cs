namespace AwakenServer.Common;

public enum WorkerBusinessType
{
    LiquidityEvent,
    SwapEvent,
    SyncEvent,
    TradePairEvent,
    TradePairUpdate,
    MarketSnapshot,
    TransactionRevert,
    TradeRecordUpdate,
}
