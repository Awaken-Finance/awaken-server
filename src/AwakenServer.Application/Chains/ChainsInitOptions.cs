using System.Collections.Generic;

namespace AwakenServer.Chains;

public class ChainsInitOptions
{
    public string PrivateKey { get; set; }
    public List<ChainDto> Chains { get; set; }
}