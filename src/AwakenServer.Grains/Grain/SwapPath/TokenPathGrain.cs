using AwakenServer.Grains.Grain.Price;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.Price.TradeRecord;
using AwakenServer.Grains.Grain.Tokens.TokenPrice;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Grains.State.SwapTokenPath;
using AwakenServer.Grains.State.Trade;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using IdentityServer4.Models;
using Microsoft.Extensions.Logging;
using Nethereum.Util;
using Orleans;
using Volo.Abp.ObjectMapping;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace AwakenServer.Grains.Grain.SwapTokenPath;

public class FeeRateGraph
{
    public double FeeRate { get; set; }
    public Dictionary<string, HashSet<string>> Graph { get; set; } = new Dictionary<string, HashSet<string>>();
    public Dictionary<string, Dictionary<string, PathNode>> RelationTokenDictionary { get; set; } = new Dictionary<string, Dictionary<string, PathNode>>();
}

public class TokenPathGrain : Grain<TokenPathState>, ITokenPathGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<TokenPathGrain> _logger;
    private readonly IClusterClient _clusterClient;
    private Dictionary<double, FeeRateGraph> _feeRateGraphs { get; set; } = new Dictionary<double, FeeRateGraph>();
    
    public TokenPathGrain(IObjectMapper objectMapper,
        IClusterClient clusterClient,
        ILogger<TokenPathGrain> logger)
    {
        _objectMapper = objectMapper;
        _logger = logger;
        _clusterClient = clusterClient;
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        // fix me
        ResetCacheAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }

    private string GenCacheKey(GetTokenPathGrainDto dto)
    {
        var sortedSymbols = new List<string> { dto.StartSymbol, dto.EndSymbol };
        sortedSymbols.Sort();

        return $"{sortedSymbols[0]}/{sortedSymbols[1]}-{dto.MaxDepth}";
    }
    
    private TokenPath MakePath(FeeRateGraph feeRateGraph, List<string> path)
    {
        var pairPath = new TokenPath()
        {
            FeeRate = feeRateGraph.FeeRate
        };
        
        for (int i = 0; i < path.Count - 1; i++)
        {
            string token0 = path[i];
            string token1 = path[i + 1];
            if (feeRateGraph.RelationTokenDictionary.ContainsKey(token0) && feeRateGraph.RelationTokenDictionary[token0].ContainsKey(token1))
            {
                pairPath.Path.Add(feeRateGraph.RelationTokenDictionary[token0][token1]);
                pairPath.FullPath += feeRateGraph.RelationTokenDictionary[token0][token1].Address + (i < path.Count - 2 ? "->" : string.Empty);
            }
        }
        return pairPath;
    }

    public async Task<GrainResultDto<TokenPathResultGrainDto>> GetCachedPathAsync(GetTokenPathGrainDto dto)
    {
        var cacheKey = GenCacheKey(dto);
        if (State.PathCache.ContainsKey(cacheKey))
        {
            return new GrainResultDto<TokenPathResultGrainDto>()
            {
                Success = true,
                Data = new TokenPathResultGrainDto()
                {
                    Path = State.PathCache[cacheKey]
                }
            };
        }

        return new GrainResultDto<TokenPathResultGrainDto>()
        {
            Success = false
        };
    }

    private async Task SetCachedPathAsync(GetTokenPathGrainDto dto, List<TokenPath> paths)
    {
        var cacheKey = GenCacheKey(dto);
        State.PathCache[cacheKey] = paths;
    }
    
    public async Task<GrainResultDto<TokenPathResultGrainDto>> GetPathAsync(GetTokenPathGrainDto dto)
    {
        var cachedPathResult = await GetCachedPathAsync(dto);
        if (cachedPathResult.Success)
        {
            _logger.LogInformation($"get cached paths, input: {dto.StartSymbol}, {dto.EndSymbol}, {dto.MaxDepth}, path count: {cachedPathResult.Data.Path.Count}");
            return cachedPathResult;
        }
        
        _logger.LogInformation($"search paths, input: {dto.StartSymbol}, {dto.EndSymbol}, {dto.MaxDepth}");
        
        var pathResult = new List<TokenPath>();
        var distinctPaths = new HashSet<string>();
        foreach (var feeRateGraph in _feeRateGraphs)
        {
            var allPaths = new List<List<string>>();
            var currentPath = new List<string>();
            var visited = new HashSet<string>();
            DFS(feeRateGraph.Value.Graph, dto.StartSymbol, dto.EndSymbol, dto.MaxDepth, visited, currentPath, allPaths);
            foreach (var path in allPaths)
            {
                var tokenPath = MakePath(feeRateGraph.Value, path);
                if (!distinctPaths.Contains(tokenPath.FullPath))
                {
                    distinctPaths.Add(tokenPath.FullPath);
                    pathResult.Add(tokenPath);
                }
            }
        }
        
        await SetCachedPathAsync(dto, pathResult);
        
        return new GrainResultDto<TokenPathResultGrainDto>()
        {
            Success = true,
            Data = new TokenPathResultGrainDto()
            {
                Path = pathResult
            }
        };
    }
    
    private void DFS(Dictionary<string, HashSet<string>> graph, string current, string end, int maxDepth, HashSet<string> visited, List<string> path, List<List<string>> allPaths)
    {
        if (maxDepth < 0) return;
        
        visited.Add(current);
        path.Add(current);

        if (current == end)
        {
            allPaths.Add(new List<string>(path));
        }
        else if (graph.ContainsKey(current))
        {
            foreach (var neighbor in graph[current])
            {
                if (!visited.Contains(neighbor))
                {
                    DFS(graph, neighbor, end, maxDepth - 1, visited, path, allPaths);
                }
            }
        }

        path.RemoveAt(path.Count - 1);
        visited.Remove(current);
    }

    public async Task<GrainResultDto> SetGraphAsync(GraphDto dto)
    {
        foreach (var pair in dto.Relations)
        {
            if (!_feeRateGraphs.ContainsKey(pair.FeeRate))
            {
                _feeRateGraphs[pair.FeeRate] = new FeeRateGraph()
                {
                    FeeRate = pair.FeeRate
                };
            }

            var graph = _feeRateGraphs[pair.FeeRate];
            if (!graph.Graph.ContainsKey(pair.Token0Symbol))
                graph.Graph[pair.Token0Symbol] = new HashSet<string>();
        
            if (!graph.Graph.ContainsKey(pair.Token1Symbol))
                graph.Graph[pair.Token1Symbol] = new HashSet<string>();
            
            if (!graph.RelationTokenDictionary.ContainsKey(pair.Token0Symbol))
                graph.RelationTokenDictionary[pair.Token0Symbol] = new Dictionary<string, PathNode>();
            
            if (!graph.RelationTokenDictionary.ContainsKey(pair.Token1Symbol))
                graph.RelationTokenDictionary[pair.Token1Symbol] = new Dictionary<string, PathNode>();
        
            graph.Graph[pair.Token0Symbol].Add(pair.Token1Symbol);
            graph.Graph[pair.Token1Symbol].Add(pair.Token0Symbol);

            graph.RelationTokenDictionary[pair.Token0Symbol][pair.Token1Symbol] = new PathNode()
            {
                Token0Symbol = pair.Token0Symbol,
                Token1Symbol = pair.Token1Symbol,
                Address = pair.Address,
                FeeRate = pair.FeeRate
            };
            graph.RelationTokenDictionary[pair.Token1Symbol][pair.Token0Symbol] = new PathNode()
            {
                Token0Symbol = pair.Token0Symbol,
                Token1Symbol = pair.Token1Symbol,
                Address = pair.Address,
                FeeRate = pair.FeeRate
            };
        }
        
        return new GrainResultDto()
        {
            Success = true
        };
    }
    
    public async Task<GrainResultDto<long>> ResetCacheAsync()
    {
        var cacheCount = State.PathCache.Count;
        State.PathCache.Clear();
        _logger.LogInformation($"clear path cache, remove count: {cacheCount}, now count: {State.PathCache.Count}");
        return new GrainResultDto<long>
        {
            Success = true,
            Data = cacheCount
        };
    }
}