using AwakenServer.Activity;

namespace AwakenServer.Grains.Grain.Activity;

[GenerateSerializer]
public class JoinRecordGrainDto
{
    [Id(0)] 
    public Guid Id { get; set; }
    [Id(1)] 
    public string ChainId { get; set; }
    [Id(2)] 
    public int ActivityId { get; set; }
    [Id(3)] 
    public string Message { get; set; }
    [Id(4)] 
    public string Signature { get; set; }
    [Id(5)] 
    public string PublicKey { get; set; }
    [Id(6)] 
    public string Address { get; set; }
}