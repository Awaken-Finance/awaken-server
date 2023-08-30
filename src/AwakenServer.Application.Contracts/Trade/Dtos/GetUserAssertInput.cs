using System.ComponentModel.DataAnnotations;

namespace AwakenServer.Trade.Dtos
{
    public class GetUserAssertInput
    {
        [Required]
        public string ChainId { get; set; }
        [Required]
        public string Address { get; set; }
    }
}