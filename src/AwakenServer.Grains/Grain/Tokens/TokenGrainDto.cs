using System;
using Nest;

namespace AwakenServer.Grains.Grain.Tokens;

[GenerateSerializer]
public class TokenGrainDto
{
    [Keyword][Id(0)] public Guid Id { get; set; }

    [Keyword][Id(1)] public string Address { get; set; }

    [Keyword][Id(2)] public string Symbol { get; set; }
    
    [Keyword][Id(3)] public string ChainId { get; set; }

    [Id(4)]
    public int Decimals { get; set; }
    [Id(5)]
    public string ImageUri { get; set; }
    
    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(Address) && string.IsNullOrEmpty(Symbol) && string.IsNullOrEmpty(ChainId);
    }
}