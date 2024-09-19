using System.Collections.Generic;
using AwakenServer.Tokens;

namespace AwakenServer.StatInfo.Dtos;

public class TokenPriceDto
{
    public TokenDto Token { get; set; }
    public List<StatInfoPriceDto> Items { get; set; } = new ();
}