using System;
using System.ComponentModel.DataAnnotations;

namespace AwakenServer.IDO.Dtos
{
    public class GetUserAssetInput
    {
        [Required]
        public string ChainId { get; set; }
        [Required]
        public string User { get; set; }
    }
}