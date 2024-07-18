using AwakenServer.Grains.Grain.Route;
using AwakenServer.Tokens;

namespace AwakenServer.Grains.State.Route;

public class RouteState
{
    public Dictionary<string, List<SwapRoute>> RouteCache { get; set; } = new ();

}