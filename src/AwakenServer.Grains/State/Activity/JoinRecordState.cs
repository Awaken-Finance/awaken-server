
namespace AwakenServer.Grains.State.Activity;

public class JoinRecordState
{
    public int ActivityId { get; set; }
    public string Message { get; set; }
    public string Signature { get; set; }
    public string PublicKey { get; set; }
    public string Address { get; set; }
}