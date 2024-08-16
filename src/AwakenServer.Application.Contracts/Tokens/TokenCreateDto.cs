using System;
using System.ComponentModel.DataAnnotations;

namespace AwakenServer.Tokens
{
    public class TokenCreateDto
    {
        public Guid Id { get; set; }
        public string ChainId { get; set; }
        public string Address { get; set; }
        public string Symbol { get; set; }
        public int Decimals { get; set; }

        public string ImageUri { get; set; }
        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(ChainId) && string.IsNullOrEmpty(Address) && string.IsNullOrEmpty(Symbol);
        }
    }
}