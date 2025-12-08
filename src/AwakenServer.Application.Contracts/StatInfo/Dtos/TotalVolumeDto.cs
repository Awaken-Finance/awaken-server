using System.Collections.Generic;

namespace AwakenServer.StatInfo.Dtos;

public class TotalVolumeDto
{
    public double TotalVolumeInUsd { get; set; }
    public List<StatInfoVolumeDto> Items { get; set; } = new ();
}