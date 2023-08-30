namespace AwakenServer.Debits.DebitAppDto
{
    public class GetNetApyInput
    {
        public string[] BorrowBalances { get; set; }
        public string[] SupplyBalances { get; set; }
        public string[] BorrowRate { get; set; }
        public string[] SupplyRate { get; set; }
    }
}