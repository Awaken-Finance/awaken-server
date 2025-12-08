using System;

namespace AwakenServer.Tokens
{
    public class GetTokenInput
    {
        public Guid Id { get; set; }
        public string ChainId { get; set; }
        public string Address { get; set; }
        public string Symbol { get; set; }
    }
}