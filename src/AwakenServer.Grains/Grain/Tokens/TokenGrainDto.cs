using System;
using Nest;

namespace AwakenServer.Grains.Grain.Tokens;

public class TokenGrainDto
{
    [Keyword] public Guid Id { get; set; }

    [Keyword] public string Address { get; set; }

    [Keyword] public string Symbol { get; set; }
    
    [Keyword] public string ChainId { get; set; }

    public int Decimals { get; set; }
    public string ImageUri { get; set; }
    
    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(Address) && string.IsNullOrEmpty(Symbol) && string.IsNullOrEmpty(ChainId);
    }
}