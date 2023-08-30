using System;
using System.ComponentModel.DataAnnotations;

namespace AwakenServer.IDO.Dtos
{
    public class GetUserPublicOfferingInfoInput : PageInputBase
    {
        [Required]
        public string ChainId { get; set; }
        [Required]
        public string User { get; set; }
    }
}