namespace AwakenServer.Grains.State.Activity;

[GenerateSerializer]
public class UserActivityState
{
    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public int ActivityId { get; set; }
    [Id(2)]
    public string Address { get; set; }
    [Id(3)]
    public double TotalPoint { get; set; }
    [Id(4)]
    public long LastUpdateTime { get; set; }
}