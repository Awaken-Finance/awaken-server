using System.Collections.Generic;
using AwakenServer.Trade.Dtos;

namespace AwakenServer.StatInfo.Dtos;

public class PoolVolumeDto
{
    public TradePairWithTokenDto TradePair { get; set; }
    public double TotalVolumeInUsd { get; set; }
    public List<StatInfoVolumeDto> Items { get; set; } = new ();
}