namespace AwakenServer.Common;

public enum WorkerBusinessType
{
    LiquidityEvent,
    SwapEvent,
    SyncEvent,
    TradePairEvent,
    TradePairUpdate,
    TransactionRevert,
    PortfolioEvent,
    InternalTokenPriceUpdate,
    UserLiquidityUpdate,
    DataCleanup,
    NewVersionPortfolioEvent,
    NewSwapEvent,
    StatInfoIndexEvent,
    NewVersionStatInfoIndexEvent,
    StatInfoUpdateEvent,
    NewVersionStatInfoUpdateEvent,
    ActivityEvent
}
