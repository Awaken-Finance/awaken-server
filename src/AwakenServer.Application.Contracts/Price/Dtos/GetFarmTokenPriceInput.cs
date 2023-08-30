using System;
using System.ComponentModel.DataAnnotations;

namespace AwakenServer.Price.Dtos
{
    public class GetFarmTokenPriceInput
    {
        public string ChainId { get; set; }
        
        [MaxLength(1000)]
        public string[] TokenAddresses { get; set; }
    }
}