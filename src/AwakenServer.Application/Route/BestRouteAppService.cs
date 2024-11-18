using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using AutoMapper;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.Route;
using AwakenServer.Price;
using AwakenServer.Route.Dtos;
using AwakenServer.Tokens;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Index;
using Microsoft.Extensions.Caching.Distributed;
using Nest;
using Newtonsoft.Json;
using Orleans;
using Serilog;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Volo.Abp.ObjectMapping;
using PercentRouteDto = AwakenServer.Route.Dtos.PercentRouteDto;
using Token = Nest.Token;
using TradePair = AwakenServer.Trade.Index.TradePair;

namespace AwakenServer.Route
{
    [RemoteService(IsEnabled = false)]
    public class BestRoutesAppService : ApplicationService, IBestRoutesAppService
    {
        private readonly IClusterClient _clusterClient;
        private readonly IObjectMapper _objectMapper;
        private readonly ILogger _logger;
        private readonly INESTRepository<TradePair, Guid> _tradePairIndexRepository;
        private readonly IDistributedCache<List<string>> _routeGrainIdsCache;
        private const string RouteGrainIdsCachePrefix = "RouteGrainIds";
        
        public int MaxDepth { get; set; } = 3;
        public int MinSplits { get; set; } = 1;
        public int DistributionPercent { get; set; } = 5;
        public int FeeRateMax = 10000;

        public BestRoutesAppService(
            IClusterClient clusterClient,
            IObjectMapper objectMapper,
            INESTRepository<TradePair, Guid> tradePairIndexRepository,
            IDistributedCache<List<string>> routeGrainIdsCache)
        {
            _logger = Log.ForContext<BestRoutesAppService>();
            _clusterClient = clusterClient;
            _objectMapper = objectMapper;
            _tradePairIndexRepository = tradePairIndexRepository;
            _routeGrainIdsCache = routeGrainIdsCache;
        }

        private string GenRouteGrainId(string chainId, string symbolBegin, string symbolEnd, int maxDepth)
        {
            return $"{chainId}/{symbolBegin}/{symbolEnd}/{maxDepth}";
        }
        
        private async Task<List<TradePairWithToken>> GetListAsync(string chainId)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<TradePair>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));
            QueryContainer Filter(QueryContainerDescriptor<TradePair> f) => f.Bool(b => b.Must(mustQuery));
            var list = await _tradePairIndexRepository.GetListAsync(Filter);
            return _objectMapper.Map<List<TradePair>, List<TradePairWithToken>>(list.Item2);
        }

        public virtual async Task<int> UpdateGrainIdsCacheKeyAsync(string chainId)
        {
            var oldCacheKey = chainId;
            var grainIdsCache = await _routeGrainIdsCache.GetAsync(oldCacheKey);
            if (grainIdsCache != null)
            {
                var newCacheKey = $"{RouteGrainIdsCachePrefix}:{chainId}";
                var newCache = await _routeGrainIdsCache.GetAsync(newCacheKey);
                if (newCache != null)
                {
                    foreach (var grainId in newCache)
                    {
                        if (!grainIdsCache.Contains(grainId))
                        {
                            grainIdsCache.Add(grainId);
                        }
                    }
                }
                await _routeGrainIdsCache.SetAsync(newCacheKey, grainIdsCache, new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(PriceOptions.PriceSuperLongExpirationTime)
                });
                
                // remove old
                await _routeGrainIdsCache.RemoveAsync(oldCacheKey);
                _logger.Information($"UpdateGrainIdsCacheKeyAsync, oldKey: {oldCacheKey}, newKey: {newCacheKey}, data: {JsonConvert.SerializeObject(grainIdsCache)}");
                return grainIdsCache.Count;
            }

            return 0;
        }
        
        public virtual async Task ResetRoutesCacheAsync(string chainId)
        {
            var cacheKey = $"{RouteGrainIdsCachePrefix}:{chainId}";
            var grainIdsCache = await _routeGrainIdsCache.GetAsync(cacheKey);
            if (grainIdsCache != null)
            {
                foreach (var grainId in grainIdsCache)
                {
                    var grain = _clusterClient.GetGrain<IRouteGrain>(grainId);
                    var resetResult = await grain.ResetCacheAsync();
                    _logger.Information($"clear route cache, cacheKey: {cacheKey}, chain: {chainId}, route grain: {grainId}, count: {resetResult.Data}");
                    var searchRequest = grainId.Split('/');
                    if (searchRequest.Length == 4)
                    {
                        await GetRoutesAsync(searchRequest[0], searchRequest[1], searchRequest[2], int.Parse(searchRequest[3]));
                    }
                }
            }
        }
        
        public async Task<List<SwapRoute>> GetRoutesAsync(string chainId, string symbolIn,
            string symbolOut, int maxDepth)
        {
            var grainId = GenRouteGrainId(chainId, symbolIn, symbolOut, maxDepth);
            var grain = _clusterClient.GetGrain<IRouteGrain>(grainId);

            var cachedResult = await grain.GetRoutesAsync(new GetRoutesGrainDto()
            {
                ChainId = chainId,
                MaxDepth = maxDepth,
                SymbolBegin = symbolIn,
                SymbolEnd = symbolOut
            });
            if (cachedResult.Success)
            {
                _logger.Information(
                    $"get route from cache, start: {symbolIn}, end:{symbolOut}, maxDepth:{maxDepth}, path count: {cachedResult.Data.Routes.Count}");
                return cachedResult.Data.Routes;
            }

            var pairs = await GetListAsync(chainId);
            _logger.Information(
                $"get route do search, chain: {chainId}, get relations from chain trade pairs, count: {pairs.Count}");

            var result =
                await grain.SearchRoutesAsync(new SearchRoutesGrainDto()
                {
                    ChainId = chainId,
                    MaxDepth = maxDepth,
                    SymbolBegin = symbolIn,
                    SymbolEnd = symbolOut,
                    Relations = _objectMapper.Map<List<TradePairWithToken>, List<TradePairWithTokenDto>>(pairs)
                });

            if (!result.Success || result.Data == null)
            {
                _logger.Error(
                    $"get route failed, start: {symbolIn}, end:{symbolOut}, maxDepth:{maxDepth}, flag: {result.Success}");
                return new List<SwapRoute>();
            }

            var cacheKey = $"{RouteGrainIdsCachePrefix}:{chainId}";
            var routeGrainIdCache = await _routeGrainIdsCache.GetAsync(cacheKey);
            if (routeGrainIdCache == null)
            {
                routeGrainIdCache = new List<string>();
            }

            if (!routeGrainIdCache.Contains(grainId))
            {
                routeGrainIdCache.Add(grainId);
            }
            
            await _routeGrainIdsCache.SetAsync(cacheKey, routeGrainIdCache, new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(PriceOptions.PriceSuperLongExpirationTime)
            });
            
            _logger.Information(
                $"get route done, start: {symbolIn}, end:{symbolOut}, maxDepth:{maxDepth}, routes count: {result.Data.Routes.Count}, grain id cache: {String.Join(", ", routeGrainIdCache)}");
            return result.Data.Routes;
        }

        public async Task<Tuple<long, long, double>> GetReservesAsync(ConcurrentDictionary<Guid, TradePairReserve> tradePairReserveMap, Guid tradePairId, string symbolIn,
            string symbolOut)
        {
            if (!tradePairReserveMap.ContainsKey(tradePairId))
            {
                _logger.Error($"Get best route, can't find trade pair: {tradePairId}");
                return new Tuple<long, long, double>(0, 0, 0);
            }
            
            var tradePair = tradePairReserveMap[tradePairId];
        
            if (symbolIn == tradePair.Token0Symbol)
            {
                return new Tuple<long, long, double>(tradePair.Token0Reserve, tradePair.Token1Reserve, tradePair.FeeRate);
            }

            return new Tuple<long, long, double>(tradePair.Token1Reserve, tradePair.Token0Reserve, tradePair.FeeRate);
        }

        public Tuple<bool, long> GetAmountIn(double poolFeeRate, long amountOut, long reserveIn, long reserveOut)
        {
            if (amountOut <= 0)
            {
                return new Tuple<bool, long>(false, -1);
            }

            if (!(reserveIn > 0 && reserveOut > 0 && reserveOut > amountOut))
            {
                return new Tuple<bool, long>(false, -1);
            }
            
            var feeRate = poolFeeRate * FeeRateMax;
            var feeRateRest = FeeRateMax - feeRate;
            var numerator = (double)reserveIn * amountOut * FeeRateMax;
            var denominator = (reserveOut - amountOut) * feeRateRest;
            var amountIn = numerator / denominator + 1;

            return new Tuple<bool, long>(true, (long)Math.Floor(amountIn));
        }

        public Tuple<bool, long> GetAmountOut(double poolFeeRate, long amountIn, long reserveIn, long reserveOut)
        {
            if (amountIn <= 0)
            {
                return new Tuple<bool, long>(false, -1);
            }

            if (!(reserveIn > 0 && reserveOut > 0))
            {
                return new Tuple<bool, long>(false, -1);
            }
            
            var feeRate = poolFeeRate * FeeRateMax;
            var feeRateRest = FeeRateMax - feeRate;
            var amountInWithFee = feeRateRest * amountIn;
            var numerator = amountInWithFee * reserveOut;
            var denominator = (double)reserveIn * FeeRateMax + amountInWithFee;
            var amountOut = numerator / denominator;

            return new Tuple<bool, long>(true, (long)Math.Floor(amountOut));
        }

        public async Task<List<long>> GetAmountsInAsync(ConcurrentDictionary<Guid, TradePairReserve> tradePairReserveMap, List<string> tokens, List<Guid> tradePairs, long amountOut)
        {
            if (tokens.Count < 2)
            {
                return null;
            }

            var amounts = new List<long>() { amountOut };
            var i = tokens.Count - 1;
            var j = tradePairs.Count - 1;
            for (; i > 0 && j >= 0; i--, j--)
            {
                var symbolOut = tokens[i];
                var symbolIn = tokens[i - 1];
                var tradePairId = tradePairs[j];
                var (reserveIn, reserveOut, feeRate) = await GetReservesAsync(tradePairReserveMap, tradePairId, symbolIn, symbolOut);
                var (success, amount) = GetAmountIn(feeRate, amounts[0], reserveIn, reserveOut);
                if (!success)
                {
                    return null;
                }
                amounts.Insert(0, amount);
            }

            return amounts;
        }

        public async Task<List<long>> GetAmountsOutAsync(ConcurrentDictionary<Guid, TradePairReserve> tradePairReserveMap, List<string> tokens, List<Guid> tradePairs, long amountIn)
        {
            if (tokens.Count < 2)
            {
                return null;
            }

            var amounts = new List<long> { amountIn };
            var i = 0;
            var j = 0;
            for (; i < tokens.Count - 1 && j < tradePairs.Count; i++, j++)
            {
                var symbolIn = tokens[i];
                var symbolOut = tokens[i + 1];
                var tradePairId = tradePairs[j];
                var (reserveIn, reserveOut, feeRate) = await GetReservesAsync(tradePairReserveMap, tradePairId, symbolIn, symbolOut);
                var (success, amount) = GetAmountOut(feeRate, amounts[i], reserveIn, reserveOut);
                if (!success)
                {
                    return null;
                }
                amounts.Add(amount);
            }

            return amounts;
        }


        public List<int> GetPercents(int distributionPercent)
        {
            var percents = new List<int>();
            if (100 % distributionPercent != 0)
            {
                _logger.Error("Get best route, error in distributionPercent. Only calculate 100 percent.");
                percents.Add(100);
            }
            else
            {
                for (int i = 1; i <= 100 / distributionPercent; i++)
                {
                    percents.Add(i * distributionPercent);
                }
            }

            return percents;
        }

        public PercentSwapRoute FindFirstRouteNotUsingUsedPools(List<PercentSwapRoute> currentRoutes,
            List<PercentSwapRoute> percentRoutes, ConcurrentDictionary<string, SwapRoute> routeMap)
        {
            var currentTradePairs = new HashSet<string>();
            foreach (var currentRoute in currentRoutes)
            {
                if (routeMap.ContainsKey(currentRoute.RouteId))
                {
                    foreach (var tradePair in routeMap[currentRoute.RouteId].TradePairs)
                    {
                        currentTradePairs.Add(tradePair.Address);
                    }
                }
            }

            foreach (var route in percentRoutes)
            {
                if (routeMap.ContainsKey(route.RouteId))
                {
                    bool contains = false;
                    foreach (var tradePair in routeMap[route.RouteId].TradePairs)
                    {
                        if (currentTradePairs.Contains(tradePair.Address))
                        {
                            contains = true;
                            break;
                        }
                    }

                    if (!contains)
                    {
                        return route;
                    }
                }
            }

            return null;
        }

        [ExceptionHandler(typeof(Exception), Message = "GetBestRoutes Error", 
            TargetType = typeof(HandlerExceptionService), MethodName = nameof(HandlerExceptionService.HandleWithReturnNull))]
        public virtual async Task<BestRoutesDto> GetBestRoutesAsync(GetBestRoutesInput input)
        {
            var swapRoutes =
                await GetRoutesAsync(input.ChainId, input.SymbolIn, input.SymbolOut, MaxDepth);
            
            _logger.Information(
                $"Get best routes, all routes count: {swapRoutes.Count}");
            
            if (swapRoutes.Count <= 0)
            {
                return new BestRoutesDto()
                {
                    StatusCode = StatusCode.NoRouteFound,
                    Message = "No route found."
                };
            }
            
            var percents = GetPercents(DistributionPercent);
            var percentToSortedRoutes = new Dictionary<int, List<PercentSwapRoute>>();
            
            var crossFeeRateRoutes = new List<SwapRoute>();
            if (input.CrossRate)
            {
                crossFeeRateRoutes = swapRoutes;
            }
            else
            {
                foreach (var route in swapRoutes)
                {
                    HashSet<double> feeRates = new HashSet<double>(route.TradePairs.Select(v => v.FeeRate));
                    if (feeRates.Count == 1)
                    {
                        crossFeeRateRoutes.Add(route);
                    }
                }
            }
            
            var routeMap = new ConcurrentDictionary<string, SwapRoute>();
            var tradePairMap = new ConcurrentDictionary<Guid, TradePairReserve>();

            foreach (var crossFeeRateRoute in crossFeeRateRoutes)
            {
                routeMap[crossFeeRateRoute.FullPathStr] = crossFeeRateRoute;
                foreach (var tradePair in crossFeeRateRoute.TradePairs)
                {
                    if (tradePairMap.ContainsKey(tradePair.Id))
                    {
                        continue;
                    }
                    tradePairMap[tradePair.Id] = new TradePairReserve(){};
                }
            }
            
            var mapTasks = tradePairMap.Select(async tradePair =>
            {
                var tradePairGrain = _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(tradePair.Key));
                var pairResult = await tradePairGrain.GetAsync();
                if (!pairResult.Success)
                {
                    _logger.Error($"GetReservesAsync failed. Can not find trade pair: {tradePair.Key}, result: {pairResult.Success}, {pairResult.Data}, {pairResult.Message}");
                }

                var token0Reserve = (long)Math.Floor(pairResult.Data.ValueLocked0 * Math.Pow(10, pairResult.Data.Token0.Decimals));
                var token1Reserve = (long)Math.Floor(pairResult.Data.ValueLocked1 * Math.Pow(10, pairResult.Data.Token1.Decimals));
                tradePair.Value.FeeRate = pairResult.Data.FeeRate;
                tradePair.Value.Token0Symbol = pairResult.Data.Token0.Symbol;
                tradePair.Value.Token1Symbol = pairResult.Data.Token1.Symbol;
                tradePair.Value.Token0Reserve = token0Reserve;
                tradePair.Value.Token1Reserve = token1Reserve;
            }).ToList();

            await Task.WhenAll(mapTasks);
            
            _logger.Information(
                $"Get best routes, all cross(or not) rate routes count: {crossFeeRateRoutes.Count}");
            
            foreach (var percent in percents)
            {
                percentToSortedRoutes[percent] = new List<PercentSwapRoute>();
                var tasks = crossFeeRateRoutes.Select(async originalRoute =>
                {
                    var percentSwapRoute = new PercentSwapRoute();
                    percentSwapRoute.RouteId = originalRoute.FullPathStr;
                    var tokenSymbols = originalRoute.Tokens.Select(x => x.Symbol).ToList();
                    var tradePairIds = originalRoute.TradePairs.Select(x => x.Id).ToList();

                    percentSwapRoute.Exact = input.RouteType == RouteType.ExactIn
                        ? (long) Math.Floor(percent / 100d * input.AmountIn)
                        : (long) Math.Floor(percent / 100d * input.AmountOut);
                    var amounts = input.RouteType == RouteType.ExactIn
                        ? await GetAmountsOutAsync(tradePairMap, tokenSymbols, tradePairIds, percentSwapRoute.Exact)
                        : await GetAmountsInAsync(tradePairMap, tokenSymbols, tradePairIds, percentSwapRoute.Exact);
                    if (amounts == null)
                    {
                        return null;
                    }

                    percentSwapRoute.Amounts = amounts;
                    percentSwapRoute.Quote =
                        input.RouteType == RouteType.ExactIn ? amounts[amounts.Count - 1] : amounts[0];
                    
                    percentSwapRoute.Percent = percent;
                    return percentSwapRoute;
                }).ToList();

                var results = await Task.WhenAll(tasks);

                percentToSortedRoutes[percent] = results
                    .Where(route => route != null)
                    .ToList();
            }
            
            _logger.Information(
                $"Get best routes, begin to sort");
            
            foreach (var percentRoutes in percentToSortedRoutes)
            {
                percentToSortedRoutes[percentRoutes.Key].Sort((quoteRouteA, quoteRouteB) =>
                {
                    return input.RouteType == RouteType.ExactIn
                        ? quoteRouteB.Quote.CompareTo(quoteRouteA.Quote)
                        : quoteRouteA.Quote.CompareTo(quoteRouteB.Quote);
                });
            }
            
            percentToSortedRoutes.ToList().ForEach(kvp =>
                _logger.Information($"Get best routes, percent: {kvp.Key}, route count: {kvp.Value.Count}"));
            
            
            var bestSwaps = new PriorityQueue<PercentSwapRouteDistribution, PercentSwapRouteDistribution>(input.ResultCount, Comparer<PercentSwapRouteDistribution>.Create((quoteRouteA, quoteRouteB) =>
            {
                return input.RouteType == RouteType.ExactIn
                    ? quoteRouteA.Quote.CompareTo(quoteRouteB.Quote)
                    : quoteRouteB.Quote.CompareTo(quoteRouteA.Quote);
            }));

            for (int i = 0; i < input.ResultCount; ++i)
            {
                if (i < percentToSortedRoutes[100].Count)
                {
                    var bestSwap = new PercentSwapRouteDistribution
                    {
                        Quote = percentToSortedRoutes[100][i].Quote,
                        Distributions = new List<PercentSwapRoute> { percentToSortedRoutes[100][i] }
                    };
                    if (bestSwaps.Count >= input.ResultCount)
                    {
                        bestSwaps.EnqueueDequeue(bestSwap, bestSwap);
                    }
                    else
                    {
                        bestSwaps.Enqueue(bestSwap, bestSwap);
                    }
                }
            }

            var queue = new Queue<QueueNode>();
            for (int i = 0; i < percents.Count - 1; ++i)
            {
                var percent = percents[i];
                if (percentToSortedRoutes[percent].Count > 0)
                {
                    queue.Enqueue(new QueueNode
                    {
                        PercentIndex = i,
                        CurrentRoutes = new List<PercentSwapRoute> { percentToSortedRoutes[percent][0] },
                        RemainingPercent = 100 - percent
                    });
                }
            }

            var splits = 1;
            while (queue.Count > 0)
            {
                _logger.Information($"Get best routes, splits: {splits}, current best swaps count: {bestSwaps.Count}");
                var layer = queue.Count;
                splits++;
                if (splits > input.MaxSplits)
                {
                    _logger.Information("Max splits reached. Stopping search.");
                    break;
                }

                while (layer > 0)
                {
                    layer--;
                    var queueNode = queue.Dequeue();
                    for (var i = queueNode.PercentIndex; i >= 0; i--)
                    {
                        var percentA = percents[i];
                        if (percentA > queueNode.RemainingPercent)
                        {
                            continue;
                        }

                        if (percentToSortedRoutes[percentA].Count <= 0)
                        {
                            continue;
                        }

                        var routeWithQuoteA = FindFirstRouteNotUsingUsedPools(queueNode.CurrentRoutes,
                            percentToSortedRoutes[percentA], routeMap);
                        if (routeWithQuoteA == null)
                        {
                            continue;
                        }

                        var remainingPercentNew = queueNode.RemainingPercent - percentA;
                        var newRoutes = new List<PercentSwapRoute>(queueNode.CurrentRoutes) { routeWithQuoteA };

                        if (remainingPercentNew == 0 && splits >= MinSplits)
                        {
                            var quoteNew = newRoutes.Select(route => route.Quote).Sum();
                            var route = new PercentSwapRouteDistribution
                            {
                                Quote = quoteNew,
                                Distributions = newRoutes
                            };
                            if (bestSwaps.Count >= input.ResultCount)
                            {
                                bestSwaps.EnqueueDequeue(route, route);
                            }
                            else
                            {
                                bestSwaps.Enqueue(route, route);
                            }
                        }
                        else
                        {
                            queue.Enqueue(new QueueNode
                            {
                                PercentIndex = i,
                                CurrentRoutes = newRoutes,
                                RemainingPercent = remainingPercentNew
                            });
                        }
                    }
                }
            }

            if (bestSwaps.Count <= 0)
            {
                return new BestRoutesDto()
                {
                    StatusCode = StatusCode.InsufficientLiquidity,
                    Message = "Insufficient liquidity."
                };
            }
            
            var result = new BestRoutesDto()
            {
                StatusCode = StatusCode.Success
            };
            
            var count = input.ResultCount;
            while (count-- > 0)
            {
                if (bestSwaps.Count <= 0)
                {
                    break;
                }

                var bestRoute = bestSwaps.Dequeue();
                var amountIn = input.RouteType == RouteType.ExactIn ? input.AmountIn : bestRoute.Quote;
                var amountOut = input.RouteType == RouteType.ExactOut ? input.AmountOut : bestRoute.Quote;
                var distribution = new List<PercentRouteDto>();
                foreach (var partRoute in bestRoute.Distributions)
                {
                    var amountsStr = partRoute.Amounts.Select(x => x.ToString()).ToList();
                    var TradePairExtensios = new List<TradePairExtensionDto>();
                    foreach (var tradePair in routeMap[partRoute.RouteId].TradePairs)
                    {
                        var tradePairGrain = _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(tradePair.Id));
                        var pairResult = await tradePairGrain.GetAsync();
                        if (pairResult.Success)
                        {
                            TradePairExtensios.Add(new TradePairExtensionDto()
                            {
                                ValueLocked0 = pairResult.Data.ValueLocked0.ToString(),
                                ValueLocked1 = pairResult.Data.ValueLocked1.ToString()
                            });
                        }
                        else
                        {
                            _logger.Error($"Get best route, can't find trade pair: {tradePair.Id}");
                        }
                    }

                    if (TradePairExtensios.Count != routeMap[partRoute.RouteId].TradePairs.Count)
                    {
                        continue;
                    }
                    
                    distribution.Add(new PercentRouteDto
                    {
                        Percent = partRoute.Percent,
                        AmountIn = (input.RouteType == RouteType.ExactIn ? partRoute.Exact : partRoute.Quote).ToString(),
                        AmountOut = (input.RouteType == RouteType.ExactOut ? partRoute.Exact : partRoute.Quote).ToString(),
                        TradePairs = routeMap[partRoute.RouteId].TradePairs,
                        TradePairExtensions = TradePairExtensios,
                        Tokens = routeMap[partRoute.RouteId].Tokens,
                        FeeRates = routeMap[partRoute.RouteId].FeeRates,
                        Amounts = amountsStr
                    });
                }

                result.Routes.Insert(0, new RouteDto
                {
                    AmountIn = amountIn.ToString(),
                    AmountOut = amountOut.ToString(),
                    Splits = distribution.Count,
                    Distributions = distribution
                });
            }

            return result;
        }


        [AutoMap(typeof(SwapRoute))]
        public class PercentSwapRoute
        {
            public int Percent { get; set; }
            public long Exact { get; set; }
            public long Quote { get; set; }
            public List<long> Amounts { get; set; }
            public string RouteId { get; set; }
        }

        public class PercentSwapRouteDistribution
        {
            public long Quote { get; set; }
            public List<PercentSwapRoute> Distributions { get; set; }
        }

        public class QueueNode
        {
            public int PercentIndex { get; set; }
            public List<PercentSwapRoute> CurrentRoutes { get; set; }
            public int RemainingPercent { get; set; }
        }

        public class TradePairReserve
        {
            public string Token0Symbol { get; set; }
            public string Token1Symbol { get; set; }
            public long Token0Reserve { get; set; }
            public long Token1Reserve { get; set; }
            public double FeeRate { get; set; }
        }
    }
}