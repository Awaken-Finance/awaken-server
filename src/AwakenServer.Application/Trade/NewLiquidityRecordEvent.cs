namespace AwakenServer.Trade
{
    public class NewLiquidityRecordEvent : LiquidityRecord
    {
        public bool IsRevert { get; set; }
        public string TotalSupply { get; set; } = "0";
    }
}