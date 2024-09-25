namespace AwakenServer.Activity.Dtos;

public class RankingInfoDto
{
    public int Ranking { get; set; }
    public string Address { get; set; }
    public int RankingChange1H { get; set; }
    public long TotalPoint { get; set; }
    public int NewStatus { get; set; }
}