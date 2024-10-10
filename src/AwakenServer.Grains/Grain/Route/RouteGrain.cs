using AwakenServer.Grains.Grain.Price;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.Price.TradeRecord;
using AwakenServer.Grains.Grain.SwapTokenPath;
using AwakenServer.Grains.Grain.Tokens.TokenPrice;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Grains.State.Route;
using AwakenServer.Grains.State.SwapTokenPath;
using AwakenServer.Grains.State.Trade;
using AwakenServer.Tokens;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Microsoft.Extensions.Logging;
using Nethereum.Util;
using Orleans;
using Volo.Abp.ObjectMapping;
using JsonConvert = Newtonsoft.Json.JsonConvert;
using AwakenServer.Tokens;
using AwakenServer.Trade.Index;
using Serilog;

namespace AwakenServer.Grains.Grain.Route;

public class Graph
{
    public Dictionary<string, HashSet<string>> Adjacents { get; set; } = new();
    public Dictionary<string, TradePairWithTokenDto> RelationTradePairAddressToData { get; set; } = new();
    public Dictionary<string, TokenDto> TokenDictionary { get; set; } = new();
}

public class RawRoute
{
    public List<string> tradePairAddresses { get; set; } = new();
    public List<string> tokens { get; set; } = new();
}

public class RouteGrain : Grain<RouteState>, IRouteGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<RouteGrain> _logger;

    public RouteGrain(IObjectMapper objectMapper,
        ILogger<RouteGrain> logger)
    {
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    private void DFS(Graph graph, string relationTradePairAddress, string current, string end, int maxDepth, HashSet<string> visited, RawRoute route, List<RawRoute> allRoutes)
    {
        if (maxDepth < 0) return;
        
        visited.Add(current);
        route.tokens.Add(current);
        route.tradePairAddresses.Add(relationTradePairAddress);
        
        if (current == end)
        {
            allRoutes.Add(new RawRoute()
            {
                tradePairAddresses = new List<string>(route.tradePairAddresses),
                tokens = new List<string>(route.tokens)
            });
        }
        else if (graph.Adjacents.ContainsKey(current))
        {
            foreach (var relation in graph.Adjacents[current])
            {
                var tradePair = graph.RelationTradePairAddressToData[relation];
                var to = tradePair.Token0.Symbol == current ? tradePair.Token1.Symbol : tradePair.Token0.Symbol;
                if (!visited.Contains(to))
                {
                    DFS(graph, relation, to, end, maxDepth - 1, visited, route, allRoutes);
                }
            }
        }
        
        route.tradePairAddresses.RemoveAt(route.tradePairAddresses.Count - 1);
        route.tokens.RemoveAt(route.tokens.Count - 1);
        visited.Remove(current);
    }

    private SwapRoute FillRoute(Graph graph, RawRoute route)
    {
        var swapRoute = new SwapRoute();
        for (int i = 1; i < route.tradePairAddresses.Count; i++)
        {
            string tradePairAddress = route.tradePairAddresses[i];
            var tradePair = graph.RelationTradePairAddressToData[tradePairAddress];
            swapRoute.TradePairs.Add(tradePair);
            swapRoute.FullPathStr += tradePairAddress + (i < route.tradePairAddresses.Count - 1 ? "-" : string.Empty);
            swapRoute.FeeRates.Add(tradePair.FeeRate);
        }

        foreach (var token in route.tokens)
        {
            swapRoute.Tokens.Add(graph.TokenDictionary[token]);
        }
        
        return swapRoute;
    }
    
    private string GenCacheKey(string symbolBegin, string symbolEnd, int maxDepth)
    {
        return $"{symbolBegin}/{symbolEnd}/{maxDepth}";
    }
    
    
    public async Task<GrainResultDto<RoutesResultGrainDto>> SearchRoutesAsync(SearchRoutesGrainDto dto)
    {
        var graph = new Graph();
        foreach (var pair in dto.Relations)
        {
            graph.RelationTradePairAddressToData[pair.Address] = pair;
            if (!graph.Adjacents.ContainsKey(pair.Token0.Symbol))
            {
                graph.Adjacents[pair.Token0.Symbol] = new HashSet<string>();
            }
            if (!graph.Adjacents.ContainsKey(pair.Token1.Symbol))
            {
                graph.Adjacents[pair.Token1.Symbol] = new HashSet<string>();
            }
            graph.Adjacents[pair.Token0.Symbol].Add(pair.Address);
            graph.Adjacents[pair.Token1.Symbol].Add(pair.Address);
            if (!graph.TokenDictionary.ContainsKey(pair.Token0.Symbol))
            {
                graph.TokenDictionary[pair.Token0.Symbol] = pair.Token0;
            }
            if (!graph.TokenDictionary.ContainsKey(pair.Token1.Symbol))
            {
                graph.TokenDictionary[pair.Token1.Symbol] = pair.Token1;
            }
        }
        
        var allRoutes = new List<RawRoute>();
        var currentPath = new RawRoute();
        var visited = new HashSet<string>();
        DFS(graph, null, dto.SymbolBegin, dto.SymbolEnd, dto.MaxDepth, visited, currentPath, allRoutes);
        var swapRoutes = new List<SwapRoute>();
        var distinctPaths = new HashSet<string>();
        foreach (var route in allRoutes)
        {
            var swapRoute = FillRoute(graph, route);
            if (!distinctPaths.Contains(swapRoute.FullPathStr))
            {
                distinctPaths.Add(swapRoute.FullPathStr);
                swapRoutes.Add(swapRoute);
            }
        }
        
        var cacheKey = GenCacheKey(dto.SymbolBegin, dto.SymbolEnd, dto.MaxDepth);
        State.RouteCache[cacheKey] = swapRoutes;
        
        return new GrainResultDto<RoutesResultGrainDto>()
        {
            Success = true,
            Data = new RoutesResultGrainDto()
            {
                Routes = swapRoutes
            }
        };
    }

    public async Task<GrainResultDto<RoutesResultGrainDto>> GetRoutesAsync(GetRoutesGrainDto dto)
    {
        var cacheKey = GenCacheKey(dto.SymbolBegin, dto.SymbolEnd, dto.MaxDepth);
        if (State.RouteCache.ContainsKey(cacheKey))
        {
            return new GrainResultDto<RoutesResultGrainDto>()
            {
                Success = true,
                Data = new RoutesResultGrainDto()
                {
                    Routes = State.RouteCache[cacheKey]
                }
            };
        }

        return new GrainResultDto<RoutesResultGrainDto>()
        {
            Success = false
        };
    }

    public async Task<GrainResultDto<long>> ResetCacheAsync()
    {
        var cacheCount = State.RouteCache.Count;
        State.RouteCache.Clear();
        Log.Information($"clear route cache, grain id: {this.GetPrimaryKeyString()}, remove count: {cacheCount}, now count: {State.RouteCache.Count}");
        return new GrainResultDto<long>
        {
            Success = true,
            Data = cacheCount
        };
    }
    
}