namespace AwakenServer.Activity;

public class UserActivityInfo : ActivityBase
{
    public string Address { get; set; }
    public double TotalPoint { get; set; }
    public long LastUpdateTime { get; set; }
}