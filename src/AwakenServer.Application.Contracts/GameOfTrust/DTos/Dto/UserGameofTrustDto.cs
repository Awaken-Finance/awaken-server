namespace AwakenServer.GameOfTrust.DTos
{   
    public class UserGameofTrustDto
    {
        public GameOfTrustDto GameOfTrust { get; set; }
        public string Address { get; set; }
        public string ValueLocked { get; set; }
        public string ReceivedAmount { get; set; }
        public string ReceivedFineAmount { get; set; }
    }
}