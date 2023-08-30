using System;

namespace AwakenServer.IDO.Dtos
{
    public class GetPublicOfferingInput : PageInputBase
    {
        public string? ChainId { get; set; }
    }
}