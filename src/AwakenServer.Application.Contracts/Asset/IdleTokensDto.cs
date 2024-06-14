using System.Collections.Generic;
using AwakenServer.Tokens;

namespace AwakenServer.Asset;

public class IdleTokensDto
{
    public string TotalValueInUsd { get; set; }
    public List<IdleToken> IdleTokens { get; set; }
}

public class IdleToken
{
    public TokenDto TokenDto { get; set; }
    public string ValueInUsd { get; set; }
    public string Percent { get; set; }
}