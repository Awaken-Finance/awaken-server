using System.Collections.Generic;

namespace AwakenServer.Asset;

public class UserTokenDto
{
    public string ChainId { get; set; }
    public string Address { get; set; }
    public string Symbol { get; set; }
    public long Balance { get; set; }
}

public class UserTokenResultDto
{
    public List<UserTokenDto> GetUserTokens { get; set; }
}