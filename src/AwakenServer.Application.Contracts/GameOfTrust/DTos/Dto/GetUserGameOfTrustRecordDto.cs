namespace AwakenServer.GameOfTrust.DTos.Dto
{
    public class GetUserGameOfTrustRecordDto
    {
        public GameOfTrustDto GameOfTrust { get; set; }
        public string Address { get; set; }
        public int Type { get; set; }
        public string Amount { get; set; }
        public long Timestamp { get; set; }
        public string TransactionHash { get; set; }
    }
}