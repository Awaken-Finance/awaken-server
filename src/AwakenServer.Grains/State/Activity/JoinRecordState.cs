
namespace AwakenServer.Grains.State.Activity;

[GenerateSerializer]
public class JoinRecordState
{
    [Id(0)]
    public int ActivityId { get; set; }
    [Id(1)]
    public string Message { get; set; }
    [Id(2)]
    public string Signature { get; set; }
    [Id(3)]
    public string PublicKey { get; set; }
    [Id(4)]
    public string Address { get; set; }
}