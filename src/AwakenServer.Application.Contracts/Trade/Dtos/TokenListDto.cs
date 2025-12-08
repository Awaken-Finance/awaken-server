using System.Collections.Generic;
using AwakenServer.Tokens;

namespace AwakenServer.Trade.Dtos
{
    public class TokenListDto
    {
        public List<TokenDto> Token0 { get; set; }
        public List<TokenDto> Token1 { get; set; }
    }
}