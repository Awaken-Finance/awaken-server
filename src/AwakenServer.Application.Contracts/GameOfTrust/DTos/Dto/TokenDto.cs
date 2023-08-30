using System;

namespace AwakenServer.GameOfTrust.DTos.Dto
{
    public class TokenDto
    {   
        public Guid Id { get; set; }
        public string Address { get; set; }
        public string Symbol { get; set; }
        public int Decimals { get; set; }
    }
}