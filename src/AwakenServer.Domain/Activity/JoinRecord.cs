using Nest;

namespace AwakenServer.Activity;

public class JoinRecord : ActivityBase
{
    [Keyword] public string Message { get; set; }
    [Keyword] public string Signature { get; set; }
    [Keyword] public string PublicKey { get; set; }
    [Keyword] public string Address { get; set; }
}