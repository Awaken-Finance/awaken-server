using System;
using AwakenServer.GameOfTrust.DTos.Dto;

namespace AwakenServer.IDO.Dtos
{
    public abstract class PublicOfferingBaseDto
    {
        public Guid Id { get; set; }
        public string ChainId { get; set; }
        public long OrderRank { get; set; }
        public string TokenContractAddress { get; set; }
        public long MaxAmount { get; set; }
        public long RaiseMaxAmount { get; set; }
        public string Publisher { get; set; }
        public long StartTimestamp { get; set; }
        public long EndTimestamp { get; set; }
    }
    
    public class PublicOfferingWithTokenDto: PublicOfferingBaseDto
    {
        public TokenDto Token { get; set; }
        public TokenDto RaiseToken { get; set; }
    }
    
    public class PublicOfferingDto: PublicOfferingWithTokenDto
    {
        public long CurrentAmount { get; set; }
        public long RaiseCurrentAmount { get; set; }
    }
}