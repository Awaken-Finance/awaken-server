using System;
using AwakenServer.Trade.Dtos;

namespace AwakenServer.Asset;

public class CurrentUserLiquidityDto
{
    public string Address { get; set; }
    public Guid TradePairId { get; set; }
    public long LpTokenAmount { get; set; }
    public long Token0UnReceivedFee { get; set; }
    public long Token1UnReceivedFee { get; set; }
    public TradePairWithTokenDto TradePair { get; set; }
}