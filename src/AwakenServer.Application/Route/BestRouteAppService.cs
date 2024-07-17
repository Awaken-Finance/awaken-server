using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AutoMapper;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.Route;
using AwakenServer.Route.Dtos;
using AwakenServer.Tokens;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Microsoft.Extensions.Logging;
using Nest;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Services;
using AwakenServer.Trade.Index;
using TradePair = AwakenServer.Trade.Index.TradePair;
using IObjectMapper = Volo.Abp.ObjectMapping.IObjectMapper;
using PercentRouteDto = AwakenServer.Route.Dtos.PercentRouteDto;
using Token = AwakenServer.Tokens.Token;

namespace AwakenServer.Route
{
    [RemoteService(IsEnabled = false)]
    public class BestRoutesAppService : ApplicationService, IBestRoutesAppService
    {
        private readonly IClusterClient _clusterClient;
        private readonly IObjectMapper _objectMapper;
        private readonly ILogger<TradePairAppService> _logger;
        private readonly INESTRepository<TradePair, Guid> _tradePairIndexRepository;
        public int MaxDepth { get; set; } = 3;
        public int MinSplits { get; set; } = 1;
        public int DistributionPercent { get; set; } = 5;
        public int FeeRateMax = 10000;

        public BestRoutesAppService(
            ILogger<TradePairAppService> logger,
            IClusterClient clusterClient,
            IObjectMapper objectMapper,
            INESTRepository<TradePair, Guid> tradePairIndexRepository)
        {
            _logger = logger;
            _clusterClient = clusterClient;
            _objectMapper = objectMapper;
            _tradePairIndexRepository = tradePairIndexRepository;
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

        public async Task<List<SwapRoute>> GetRoutesAsync(string chainId, RouteType routeType, string symbolIn,
            string symbolOut, int maxDepth)
        {
            var grain = _clusterClient.GetGrain<IRouteGrain>(chainId);

            var cachedResult = await grain.GetRoutesAsync(new GetRoutesGrainDto()
            {
                ChainId = chainId,
                MaxDepth = maxDepth,
                SymbolBegin = symbolIn,
                SymbolEnd = symbolOut
            });
            if (cachedResult.Success)
            {
                _logger.LogInformation(
                    $"get route from cache, start: {symbolIn}, end:{symbolOut}, maxDepth:{maxDepth}, path count: {cachedResult.Data.Routes.Count}");
                return cachedResult.Data.Routes;
            }

            var pairs = await GetListAsync(chainId);
            _logger.LogInformation(
                $"get route do search, chain: {chainId}, get relations from chain trade pairs, count: {pairs.Count}");

            var result =
                await grain.SearchRoutesAsync(new SearchRoutesGrainDto()
                {
                    ChainId = chainId,
                    MaxDepth = maxDepth,
                    SymbolBegin = symbolIn,
                    SymbolEnd = symbolOut,
                    Relations = pairs
                });

            if (!result.Success || result.Data == null)
            {
                _logger.LogError(
                    $"get route failed, start: {symbolIn}, end:{symbolOut}, maxDepth:{maxDepth}, flag: {result.Success}");
                return new List<SwapRoute>();
            }

            _logger.LogInformation(
                $"get route done, start: {symbolIn}, end:{symbolOut}, maxDepth:{maxDepth}, routes count: {result.Data.Routes.Count}");
            return result.Data.Routes;
        }

        public async Task<Tuple<long, long, double>> GetReservesAsync(Guid tradePairId, string symbolIn,
            string symbolOut)
        {
            var tradePairGrain = _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(tradePairId));
            var pairResult = await tradePairGrain.GetAsync();
            if (!pairResult.Success)
            {
                _logger.LogInformation($"GetReservesAsync failed. Can not find trade pair: {tradePairId}");
                return new Tuple<long, long, double>(0, 0, 0);
            }

            var token0Reserve = (long)Math.Floor(pairResult.Data.ValueLocked0 * Math.Pow(10, pairResult.Data.Token0.Decimals));
            var token1Reserve = (long)Math.Floor(pairResult.Data.ValueLocked1 * Math.Pow(10, pairResult.Data.Token1.Decimals));
            if (symbolIn == pairResult.Data.Token0.Symbol)
            {
                return new Tuple<long, long, double>(token0Reserve, token1Reserve, pairResult.Data.FeeRate);
            }

            return new Tuple<long, long, double>(token1Reserve, token0Reserve, pairResult.Data.FeeRate);
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
            var denominator = reserveIn * FeeRateMax + amountInWithFee;
            var amountOut = numerator / denominator;

            return new Tuple<bool, long>(true, (long)Math.Floor(amountOut));
        }

        public async Task<List<long>> GetAmountsInAsync(List<string> tokens, List<Guid> tradePairs, long amountOut)
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
                var (reserveIn, reserveOut, feeRate) = await GetReservesAsync(tradePairId, symbolIn, symbolOut);
                var (success, amount) = GetAmountIn(feeRate, amounts[0], reserveIn, reserveOut);
                if (!success)
                {
                    return null;
                }
                amounts.Insert(0, amount);
            }

            return amounts;
        }

        public async Task<List<long>> GetAmountsOutAsync(List<string> tokens, List<Guid> tradePairs, long amountIn)
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
                var (reserveIn, reserveOut, feeRate) = await GetReservesAsync(tradePairId, symbolIn, symbolOut);
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
                _logger.LogError("Get best route, error in distributionPercent. Only calculate 100 percent.");
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
            List<PercentSwapRoute> percentRoutes)
        {
            var currentTradePairs = new HashSet<string>();
            foreach (var currentRoute in currentRoutes)
            {
                foreach (var tradePair in currentRoute.TradePairs)
                {
                    currentTradePairs.Add(tradePair.Address);
                }
            }

            foreach (var route in percentRoutes)
            {
                bool contains = false;
                foreach (var tradePair in route.TradePairs)
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

            return null;
        }

        public async Task<BestRoutesDto> GetBestRoutesAsync(GetBestRoutesInput input)
        {
            var swapRoutes =
                await GetRoutesAsync(input.ChainId, input.RouteType, input.SymbolIn, input.SymbolOut, MaxDepth);
            var percents = GetPercents(DistributionPercent);
            var percentToSortedRoutes = new Dictionary<int, List<PercentSwapRoute>>();
            
            _logger.LogInformation(
                $"Get best routes, all routes count: {swapRoutes.Count}");

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
            
            _logger.LogInformation(
                $"Get best routes, all cross(or not) rate routes count: {crossFeeRateRoutes.Count}");
            
            foreach (var percent in percents)
            {
                percentToSortedRoutes[percent] = new List<PercentSwapRoute>();
                var tasks = crossFeeRateRoutes.Select(async route =>
                {
                    var percentSwapRoute = _objectMapper.Map<SwapRoute, PercentSwapRoute>(route);
                    try
                    {
                        percentSwapRoute.Exact = input.RouteType == RouteType.ExactIn
                            ? (long)Math.Floor(percent / 100d * input.AmountIn)
                            : (long)Math.Floor(percent / 100d * input.AmountOut);
                        var tokens = percentSwapRoute.Tokens.Select(x => x.Symbol).ToList();
                        var tradePairIds = percentSwapRoute.TradePairs.Select(x => x.Id).ToList();
                        var amounts = input.RouteType == RouteType.ExactIn
                            ? await GetAmountsOutAsync(tokens, tradePairIds, percentSwapRoute.Exact)
                            : await GetAmountsInAsync(tokens, tradePairIds, percentSwapRoute.Exact);
                        if (amounts == null)
                        {
                            return null;
                        }
                        percentSwapRoute.Amounts = amounts;
                        percentSwapRoute.Quote = input.RouteType == RouteType.ExactIn ? amounts[amounts.Count - 1] : amounts[0];
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Get best routes, Exception: {e}");
                        return null;
                    }
                    percentSwapRoute.Percent = percent;
                    return percentSwapRoute;
                }).ToList();

                var results = await Task.WhenAll(tasks);

                foreach (var route in results)
                {
                    if (route != null)
                    {
                        percentToSortedRoutes[percent].Add(route);
                    }
                }
            }
            
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
                _logger.LogInformation($"Get best routes, percent: {kvp.Key}, route count: {kvp.Value.Count}"));
            
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
                _logger.LogInformation($"Get best routes, splits: {splits}, current best swaps count: {bestSwaps.Count}");
                var layer = queue.Count;
                splits++;
                if (splits > input.MaxSplits)
                {
                    _logger.LogInformation("Max splits reached. Stopping search.");
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
                            percentToSortedRoutes[percentA]);
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

            var result = new BestRoutesDto();
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
                    distribution.Add(new PercentRouteDto
                    {
                        Percent = partRoute.Percent,
                        AmountIn = input.RouteType == RouteType.ExactIn ? partRoute.Exact : partRoute.Quote,
                        AmountOut = input.RouteType == RouteType.ExactOut ? partRoute.Exact : partRoute.Quote,
                        TradePairs =
                            _objectMapper.Map<List<TradePairWithToken>, List<TradePairWithTokenDto>>(partRoute
                                .TradePairs),
                        Tokens = _objectMapper.Map<List<Token>, List<TokenDto>>(partRoute.Tokens),
                        FeeRates = partRoute.FeeRates,
                        Amounts = partRoute.Amounts
                    });
                }

                result.Routes.Insert(0, new RouteDto
                {
                    AmountIn = amountIn,
                    AmountOut = amountOut,
                    Splits = distribution.Count,
                    Distributions = distribution
                });
            }

            return result;
        }


        [AutoMap(typeof(SwapRoute))]
        public class PercentSwapRoute : SwapRoute
        {
            public int Percent { get; set; }
            public long Exact { get; set; }
            public long Quote { get; set; }
            public List<long> Amounts { get; set; }
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
    }
}