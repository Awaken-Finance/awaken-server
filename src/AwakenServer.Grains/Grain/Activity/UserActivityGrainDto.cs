using AwakenServer.Activity;

namespace AwakenServer.Grains.Grain.Activity;

[GenerateSerializer]
public class UserActivityGrainDto
{
    [Id(0)] 
    public Guid Id { get; set; }
    [Id(1)] 
    public string ChainId { get; set; }
    [Id(2)] 
    public int ActivityId { get; set; }
    [Id(3)] 
    public string Address { get; set; }
    [Id(4)] 
    public double TotalPoint { get; set; }
    [Id(5)] 
    public long LastUpdateTime { get; set; }
}