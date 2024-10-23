using System;

namespace AwakenServer.Grains.State.Tokens;

[GenerateSerializer]
public class CurrentTokenPriceState : TokenPriceBase
{
    [Id(0)]
    public DateTime PriceUpdateTime { get; set; }
}