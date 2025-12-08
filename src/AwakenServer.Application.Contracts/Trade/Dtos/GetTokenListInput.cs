using System;
using System.ComponentModel.DataAnnotations;

namespace AwakenServer.Trade.Dtos
{
    public class GetTokenListInput
    {
        [Required]
        public string ChainId { get; set; }
    }
}