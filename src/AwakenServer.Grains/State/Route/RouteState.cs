using AwakenServer.Grains.Grain.Route;
using AwakenServer.Tokens;

namespace AwakenServer.Grains.State.Route;

[GenerateSerializer]
public class RouteState
{
    [Id(0)]
    public Dictionary<string, List<SwapRoute>> RouteCache { get; set; } = new ();
}