using AwakenServer.Grains.Grain.SwapTokenPath;

namespace AwakenServer.Grains.State.SwapTokenPath;

public class TokenPathState
{
    public Dictionary<string, List<TokenPath>> PathCache { get; set; } = new Dictionary<string, List<TokenPath>>();
}