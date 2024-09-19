using System.Collections.Generic;
using AwakenServer.Tokens;
using AwakenServer.Trade.Dtos;

namespace AwakenServer.StatInfo.Dtos;

public class PoolTvlDto
{
    public TradePairWithTokenDto TradePair { get; set; }
    public List<StatInfoTvlDto> Items { get; set; } = new ();
}