using System;

namespace AwakenServer.Debits.DebitAppDto
{
    public class GetCompControllerInput
    {
        public string? ChainId { get; set; }
        public Guid? CompControllerId { get; set; }
    }
}