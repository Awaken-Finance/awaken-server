using System.Collections.Generic;

namespace AwakenServer.Activity.Dtos;

public class RankingListDto
{
    public List<RankingInfoDto> Items { get; set; } = new();
    public int ActivityId { get; set; }
}