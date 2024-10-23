using AwakenServer.Asset;

namespace AwakenServer.Grains.Grain.Asset;

[GenerateSerializer]
public class DefaultTokenGrainDto
{
    [Id(0)] public string TokenSymbol { get; set; }
}