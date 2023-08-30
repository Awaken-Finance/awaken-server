using System;
using System.ComponentModel.DataAnnotations;

namespace AwakenServer.IDO.Dtos
{
    public class GetAssetTokenInfoInput
    {
        [Required]
        public string ChainId { get; set; }
    }
}