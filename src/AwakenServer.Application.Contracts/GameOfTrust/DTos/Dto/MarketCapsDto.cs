using System;

namespace AwakenServer.GameOfTrust.DTos.Dto
{
    public class MarketCapsDto
    {  
        public Guid Id { get; set; }
        public int Pid { get; set; }
        public string UnlockMarketCap { get; set; }
    }
}