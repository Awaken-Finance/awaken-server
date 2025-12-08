using System.Collections.Generic;
using AwakenServer.Tokens;

namespace AwakenServer.SwapTokenPath.Dtos;


public class TokenPathDto
{
    public double FeeRate { get; set; }
    public List<PathNodeDto> Path { get; set; } = new ();
    public List<TokenDto> RawPath { get; set; } = new ();
}

public class PathNodeDto
{
    public TokenDto Token0 { get; set; }
    public TokenDto Token1 { get; set; }
    public string Address { get; set; }
    public double FeeRate { get; set; }
}