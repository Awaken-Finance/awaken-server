using System;

namespace AwakenServer.GameOfTrust.DTos.Input
{
    public class GetMarketCapsInput
    {
        public string ChainId { get; set; }
        public string DepositTokenSymbol { get; set; }
        public string HarvestTokenSymbol { get; set; }
    }
}