namespace AwakenServer.Activity;

public class JoinRecord : ActivityBase
{
    public string Message { get; set; }
    public string Signature { get; set; }
    public string PublicKey { get; set; }
    public string Address { get; set; }
}