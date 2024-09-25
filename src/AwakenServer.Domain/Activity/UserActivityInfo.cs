namespace AwakenServer.Activity;

public class UserActivityInfo : ActivityBase
{
    public string Address { get; set; }
    public long TotalPoint { get; set; }
    public long LastUpdateTime { get; set; }
}