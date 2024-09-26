using Nest;

namespace AwakenServer.Activity;

public class UserActivityInfo : ActivityBase
{
    [Keyword] public string Address { get; set; }
    public double TotalPoint { get; set; }
    public long LastUpdateTime { get; set; }
}