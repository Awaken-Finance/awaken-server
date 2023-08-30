using System;
using AwakenServer.Tokens;

namespace AwakenServer.Debits
{
    public class DebitTokenDto: TokenDto
    {
        public decimal TokenPrice { get; set; }
    }
    public class CTokenBaseDto: TokenDto
    {
        public Guid CompControllerId { get; set; }
    }
}