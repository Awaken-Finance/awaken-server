namespace AwakenServer.Grains.State.Activity;

public class UserActivityState
{
    public Guid Id { get; set; }
    public int ActivityId { get; set; }
    public string Address { get; set; }
    public double TotalPoint { get; set; }
    public long LastUpdateTime { get; set; }
}