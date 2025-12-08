using System.ComponentModel.DataAnnotations;

namespace AwakenServer.Trade.Dtos
{
    public class GetUserAssetInput
    {
        [Required]
        public string ChainId { get; set; }
        [Required]
        public string Address { get; set; }
    }
}