using System;
using System.ComponentModel.DataAnnotations;

namespace AwakenServer.Price.Dtos
{
    public class GetPricesInput
    {
        public string ChainId { get; set; }
        
        [MaxLength(1000)]
        public Guid[] TokenIds { get; set; }
    }
}