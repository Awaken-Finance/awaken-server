using System;

namespace AwakenServer.Price.Dtos
{
    public class FarmPriceDto
    {
        public string ChainId { get; set; }
        public string TokenSymbol { get; set; }
        public string TokenAddress { get; set; }
            
        public string Price { get; set; }
    }
}