namespace AwakenServer.Trade
{
    public class NewLiquidityRecordEvent : LiquidityRecord
    {
        public bool IsRevert { get; set; }
    }
}