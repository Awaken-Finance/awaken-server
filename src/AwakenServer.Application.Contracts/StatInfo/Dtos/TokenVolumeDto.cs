using System.Collections.Generic;
using AwakenServer.Tokens;

namespace AwakenServer.StatInfo.Dtos;

public class TokenVolumeDto
{
    public TokenDto Token { get; set; }
    public double TotalVolumeInUsd { get; set; }
    public List<StatInfoVolumeDto> Items { get; set; } = new ();
}