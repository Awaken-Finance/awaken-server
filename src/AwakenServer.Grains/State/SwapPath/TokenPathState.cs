using AwakenServer.Grains.Grain.SwapTokenPath;

namespace AwakenServer.Grains.State.SwapTokenPath;

[GenerateSerializer]
public class TokenPathState
{
    [Id(0)]
    public Dictionary<string, List<TokenPath>> PathCache { get; set; } = new Dictionary<string, List<TokenPath>>();
}