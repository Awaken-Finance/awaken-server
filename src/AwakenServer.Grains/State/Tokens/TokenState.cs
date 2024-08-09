using System;
using Nest;

namespace AwakenServer.Grains.State.Tokens;

public class TokenState
{
    public Guid Id { get; set; }

    public string Address { get; set; }

    public string Symbol { get; set; }
    
    public string ChainId { get; set; }

    public int Decimals { get; set; }
    public string ImageUri { get; set; }
    
    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(Address) && string.IsNullOrEmpty(Symbol) && string.IsNullOrEmpty(ChainId);
    }
}