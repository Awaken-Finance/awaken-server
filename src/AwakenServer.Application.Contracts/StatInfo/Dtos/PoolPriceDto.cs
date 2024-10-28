using System.Collections.Generic;
using AwakenServer.Trade.Dtos;

namespace AwakenServer.StatInfo.Dtos;

public class PoolPriceDto
{
    public TradePairWithTokenDto TradePair { get; set; }
    public List<StatInfoPriceDto> Items { get; set; } = new ();
}