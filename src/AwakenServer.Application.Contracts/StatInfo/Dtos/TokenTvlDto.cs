using System.Collections.Generic;
using AwakenServer.Tokens;

namespace AwakenServer.StatInfo.Dtos;

public class TokenTvlDto
{
    public TokenDto Token { get; set; }
    public List<StatInfoTvlDto> Items { get; set; } = new ();
}