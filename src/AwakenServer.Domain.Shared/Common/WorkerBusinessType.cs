namespace AwakenServer.Common;

public enum WorkerBusinessType
{
    LiquidityEvent,
    SwapEvent,
    SyncEvent,
    TradePairEvent,
    TradeRecordRevert, 
    TradePairUpdate,
    TradePairTotalSupplyUpdate,
    MarketSnapshot,
    TransactionRevert,
    TradeRecordUpdate,
    TradePairPriceUpdate
}
