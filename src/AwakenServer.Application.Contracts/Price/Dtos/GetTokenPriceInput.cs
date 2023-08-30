using System;

namespace AwakenServer.Price.Dtos
{
    public class GetTokenPriceInput
    {
        public string ChainId { get; set; }
        
        public Guid? TokenId { get; set; }
        
        public string TokenAddress { get; set; }
        
        public string Symbol { get; set; }
    }
}