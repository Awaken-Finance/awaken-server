using System.Collections.Generic;

namespace AwakenServer.Trade.Dtos;

public class UserLiquidityPageResultDto
{
    public long TotalCount { get; set; }
    public List<UserLiquidityDto> Data { get; set; }
}

public class UserLiquidityResultDto
{
    public UserLiquidityPageResultDto UserLiquidity { get; set; }
}

public class UserLiquidityDto
{
    public string ChainId { get; set; }
    public string Pair { get; set; }
    public string Address { get; set; }
    public long LpTokenAmount { get; set; }
}