using System;
using Nest;

namespace AwakenServer.Grains.State.Tokens;

[GenerateSerializer]
public class TokenState
{
    [Id(0)]
    public Guid Id { get; set; }

    [Id(1)]
    public string Address { get; set; }

    [Id(2)]
    public string Symbol { get; set; }

    [Id(3)]
    public string ChainId { get; set; }

    [Id(4)]
    public int Decimals { get; set; }
    [Id(5)]
    public string ImageUri { get; set; }
    
    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(Address) && string.IsNullOrEmpty(Symbol) && string.IsNullOrEmpty(ChainId);
    }
}