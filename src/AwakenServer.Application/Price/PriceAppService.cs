using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Grains.Grain.Tokens.TokenPrice;
using AwakenServer.Price.Dtos;
using AwakenServer.Tokens;
using AwakenServer.Tokens.Dtos;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Index;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Serilog;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Volo.Abp.DistributedLocking;
using Index = System.Index;
using IndexTradePair = AwakenServer.Trade.Index.TradePair;
using JsonConvert = Newtonsoft.Json.JsonConvert;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace AwakenServer.Price
{
    [RemoteService(IsEnabled = false)]
    public class PriceAppService : ApplicationService, IPriceAppService
    {
        private readonly IDistributedCache<PriceDto> _priceCache;
        private readonly IDistributedCache<PriceDto> _internalPriceCache;
        private readonly ITokenPriceProvider _tokenPriceProvider;
        private readonly IOptionsSnapshot<TokenPriceOptions> _tokenPriceOptions;
        private readonly INESTRepository<IndexTradePair, Guid> _tradePairIndexRepository;
        private readonly IDistributedCache<TokenPricingMap> _tokenPricingMapCache;
        private readonly ILogger<PriceAppService> _logger;
        private readonly IAbpDistributedLock _distributedLock;
        
        public PriceAppService(IDistributedCache<PriceDto> priceCache,
            IDistributedCache<PriceDto> internalPriceCache,
            ITokenPriceProvider tokenPriceProvider,
            IOptionsSnapshot<TokenPriceOptions> options,
            INESTRepository<IndexTradePair, Guid> tradePairIndexRepository,
            ILogger<PriceAppService> logger,
            IDistributedCache<TokenPricingMap> tokenPricingMap,
            IAbpDistributedLock distributedLock)
        {
            _priceCache = priceCache;
            _tokenPriceProvider = tokenPriceProvider;
            _tokenPriceOptions = options;
            _tradePairIndexRepository = tradePairIndexRepository;
            _logger = logger;
            _tokenPricingMapCache = tokenPricingMap;
            _internalPriceCache = internalPriceCache;
            _distributedLock = distributedLock;
        }

        public async Task<string> GetTokenPriceAsync(GetTokenPriceInput input)
        {
            if (string.IsNullOrWhiteSpace(input.Symbol)) return "0";
            var result = await GetTokenPriceListAsync(new List<string>{ input.Symbol });
            if (result.Items.Count == 0) return "0";
            else return result.Items[0].PriceInUsd.ToString();
        }

        private async Task<decimal> GetApiUsdtPriceAsync(string time)
        {
            if (String.IsNullOrEmpty(time))
            {
                return await _tokenPriceProvider.GetPriceAsync(PriceOptions.UsdtPricePair);
            }
           
            return await _tokenPriceProvider.GetHistoryPriceAsync(PriceOptions.UsdtPricePair, time);
        }

        [ExceptionHandler(typeof(Exception),
            LogLevel = LogLevel.Error, TargetType = typeof(HandlerExceptionService), MethodName = nameof(HandlerExceptionService.HandleWithReturnNull))]
        protected virtual string GetTokenApiName(string symbol)
        {
            if (String.IsNullOrEmpty(symbol))
            {
                return null;
            }
            
            _tokenPriceOptions.Value.PriceTokenMapping.TryGetValue(symbol.ToUpper(), out var priceTradePair);
            return priceTradePair;
        }
        
        private async Task<decimal> ProcessTokenPrice(string symbol, decimal rawPrice, string time)
        {
            if (_tokenPriceOptions.Value.UsdtPriceTokens.Contains(symbol))
            {
                var usdtPrice = await GetApiUsdtPriceAsync(time);
                return rawPrice * usdtPrice;
            }

            return rawPrice;
        }
        
        
        private async Task<decimal> GetPriceAsync(string symbol)
        {
            var pair = GetTokenApiName(symbol);
            if (String.IsNullOrEmpty(pair))
            {
                throw new Exception($"Get token price symbol: {symbol}, nonexistent mapping result price: 0");
            }
            
            var rawPrice = await _tokenPriceProvider.GetPriceAsync(pair);
            var result = await ProcessTokenPrice(symbol, rawPrice, null);
            Log.Information($"Get token price symbol: {symbol}, pair: {pair}, rawPrice: {rawPrice}, result price: {result}");
            return result;
        }

        [ExceptionHandler(typeof(Exception), 
            LogLevel = LogLevel.Error, TargetType = typeof(HandlerExceptionService), MethodName = nameof(HandlerExceptionService.HandleWithReturn0))]
        protected virtual async Task<decimal> GetHistoryPriceAsync(string symbol, string time)
        {
            var pair = GetTokenApiName(symbol);
            if (String.IsNullOrEmpty(pair))
            {
                Log.Information($"Get history token price symbol: {symbol}, nonexistent mapping result price: 0");
                return 0;
            }
            
            var rawPrice = await _tokenPriceProvider.GetHistoryPriceAsync(pair, time);
            var result = await ProcessTokenPrice(symbol, rawPrice, time);
            
            Log.Information($"Get history token price symbol: {symbol}, pair: {pair}, time: {time}, rawPrice: {rawPrice}, result price: {result}");
            
            return result;
        }

        private bool IsNeedFetchPrice(PriceDto priceDto)
        {
            return priceDto.PriceInUsd == PriceOptions.DefaultPriceValue ||
                   priceDto.PriceUpdateTime.AddSeconds(_tokenPriceOptions.Value.PriceExpirationTimeSeconds) <= DateTime.UtcNow;
        }
        
        
        public async Task<TokenPriceDataDto> GetApiTokenPriceAsync(string symbol)
        {
            var key = $"{PriceOptions.PriceCachePrefix}:{symbol}";
            var price = await _priceCache.GetOrAddAsync(key, async () => new PriceDto());
                    
            if (IsNeedFetchPrice(price))
            {
                try
                {
                    price.PriceInUsd = await GetPriceAsync(symbol);
                    price.PriceUpdateTime = DateTime.UtcNow;
                    await _priceCache.SetAsync(key, price, new DistributedCacheEntryOptions
                    {
                        AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(PriceOptions.PriceSuperLongExpirationTime)
                    });
                }
                catch (Exception e)
                {
                    // TODO: Remove this code in the next version (v2.2)
                    // This code is temporarily added to fix historical data issues.
                    if (price.PriceUpdateTime == DateTime.MinValue)
                    {
                        price.PriceUpdateTime = DateTime.UtcNow.AddHours(-1);
                        await _priceCache.SetAsync(key, price, new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(PriceOptions.PriceSuperLongExpirationTime)
                        });
                    }
                    if (price.PriceInUsd == PriceOptions.DefaultPriceValue)
                    {
                        price.PriceInUsd = 0;
                        await _priceCache.SetAsync(key, price, new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(PriceOptions.PriceSuperLongExpirationTime)
                        });
                    }
                    Log.Error(e, $"Get token price symbol: {symbol} failed. Return old data price: {price.PriceInUsd}");
                }
            }
                    
            Log.Information($"Get token price symbol: {symbol}, return price: {price.PriceInUsd}");
            return new TokenPriceDataDto
            {
                Symbol = symbol,
                PriceInUsd = price.PriceInUsd
            };
        }
        
        private async Task<TokenPriceDataDto> GetInternalTokenPriceAsync(string symbol)
        {
            var key = $"{PriceOptions.InternalPriceCachePrefix}:{symbol}";
            var internalPriceDto = await _internalPriceCache.GetAsync(key);
            if (internalPriceDto != null)
            {
                Log.Information($"Get internal token price symbol: {symbol}, return price: {internalPriceDto.PriceInUsd}");
                return new TokenPriceDataDto()
                {
                    Symbol = symbol,
                    PriceInUsd = internalPriceDto.PriceInUsd
                };
            }
            
            Log.Information($"Get internal token price symbol: {symbol}, return price: 0");
            return new TokenPriceDataDto()
            {
                Symbol = symbol,
                PriceInUsd = 0
            };
        }
        
        
        public async Task<TokenPriceDataDto> GetApiHistoryTokenPriceAsync(GetTokenHistoryPriceInput input)
        {
            var time = input.DateTime.ToString("dd-MM-yyyy");
            if (input.Symbol.IsNullOrEmpty())
            {
                return new TokenPriceDataDto();
            }

            var key = $"{PriceOptions.PriceHistoryCachePrefix}:{input.Symbol}:{time}";
            var price = await _priceCache.GetOrAddAsync(key, async () => new PriceDto());
                    
            if (IsNeedFetchPrice(price))
            {
                price.PriceInUsd = await GetHistoryPriceAsync(input.Symbol, time);
                price.PriceUpdateTime = DateTime.UtcNow;
                await _priceCache.SetAsync(key, price, new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(PriceOptions.PriceSuperLongExpirationTime)
                });
            }
                    
            Log.Information($"Get history token price symbol: {input.Symbol}, time: {time}, return price: {price.PriceInUsd}");
            
            return new TokenPriceDataDto
            {
                Symbol = input.Symbol,
                PriceInUsd = price.PriceInUsd
            };
        }
        
        private async Task<TokenPriceDataDto> GetInternalHistoryTokenPriceAsync(GetTokenHistoryPriceInput input)
        {
            var time = input.DateTime.ToString("dd-MM-yyyy");
            var historyKey = $"{PriceOptions.InternalPriceHistoryCachePrefix}:{input.Symbol}:{time}";
            var internalPriceDto = await _internalPriceCache.GetAsync(historyKey);
            if (internalPriceDto != null)
            {
                Log.Information($"Get internal history token price symbol: {input.Symbol}, return price: {internalPriceDto.PriceInUsd}");
                return new TokenPriceDataDto()
                {
                    Symbol = input.Symbol,
                    PriceInUsd = internalPriceDto.PriceInUsd
                };
            }
            
            
            Log.Information($"Get internal history token price symbol: {input.Symbol}, return price: 0");
            return new TokenPriceDataDto()
            {
                Symbol = input.Symbol,
                PriceInUsd = 0
            };
        }
        
        [ExceptionHandler(typeof(Exception), 
            LogOnly = true)]
        public async Task<ListResultDto<TokenPriceDataDto>> GetTokenPriceListAsync(List<string> symbols)
        {
            var result = new List<TokenPriceDataDto>();
            if (symbols.Count == 0)
            {
                return new ListResultDto<TokenPriceDataDto>();
            }

            var symbolList = symbols.Distinct(StringComparer.InvariantCultureIgnoreCase).ToList();
            for (var i = 0; i < symbolList.Count; i++)
            {
                var tokenApiName = GetTokenApiName(symbolList[i]);
                if (!string.IsNullOrEmpty(tokenApiName))
                {
                    var price = await GetApiTokenPriceAsync(symbolList[i]);
                    result.Add(price);
                }
                else
                {
                    result.Add(await GetInternalTokenPriceAsync(symbolList[i]));
                }
            }

            return new ListResultDto<TokenPriceDataDto>
            {
                Items = result
            };
        }

        [ExceptionHandler(typeof(Exception), 
            LogOnly = true)]
        public virtual async Task<TokenPriceDataDto> GetTokenHistoryPriceDataAsync(GetTokenHistoryPriceInput input)
        {
            var tokenApiName = GetTokenApiName(input.Symbol);
            if (!string.IsNullOrEmpty(tokenApiName))
            {
                return await GetApiHistoryTokenPriceAsync(input);
            }

            return await GetInternalHistoryTokenPriceAsync(input);
           
        }
        
        [ExceptionHandler(typeof(Exception), 
            LogOnly = true)]
        public async Task<ListResultDto<TokenPriceDataDto>> GetTokenHistoryPriceDataAsync(
            List<GetTokenHistoryPriceInput> inputs)
        {
            var result = new List<TokenPriceDataDto>();

            foreach (var input in inputs)
            {
                var tokenApiName = GetTokenApiName(input.Symbol);
                if (!string.IsNullOrEmpty(tokenApiName))
                {
                    var price = await GetApiHistoryTokenPriceAsync(input);
                    result.Add(price);
                }
                else
                {
                    result.Add(await GetInternalHistoryTokenPriceAsync(input));
                }
            }

            return new ListResultDto<TokenPriceDataDto>
            {
                Items = result
            };
        }
        
        public void PrintPricingNodeTree(PricingNode node, int indent = 0)
        {
            Log.Information($"{new string(' ', indent * 2)}Price Spread. Tree Depth: {node.Depth}, FromTokenSymbol: {node.FromTokenSymbol}, TokenSymbol: {node.TokenSymbol}, PriceInUsd: {node.PriceInUsd}, FromTradePairAddress: {node.FromTradePairAddress}");

            foreach (var child in node.ToTokens)
            {
                PrintPricingNodeTree(child, indent + 1);
            }
        }
        
        private Tuple<int, double> GetTokenPriority(string token, Dictionary<string, double> tokensWithUSDPrice)
        {
            var priority = _tokenPriceOptions.Value.StablecoinPriority.IndexOf(token);
            if (priority >= 0)
            {
                return new Tuple<int, double>(priority, double.MaxValue);
            }

            if (tokensWithUSDPrice.TryGetValue(token, out double tokenPrice))
            {
                return new Tuple<int, double>(int.MaxValue, tokenPrice);
            }
            
            return new Tuple<int, double>(int.MaxValue, 0);
        }
        
        private bool CanPriceFrom(string from, string to, TradePairsGraph graph)
        {
            return graph.Relations.ContainsKey(from) && graph.Relations[from].ContainsKey(to);
        }

        private TradePair GetHighestPriorityTradePair(string from, string to, TradePairsGraph graph)
        {
            if (!graph.Relations.ContainsKey(from) || !graph.Relations[from].ContainsKey(to))
            {
                return null;
            }

            var sortedTradePairs = graph.Relations[from][to]
                .OrderByDescending(tradePair => tradePair.ValueLocked0 + tradePair.ValueLocked1);

            return sortedTradePairs.FirstOrDefault();
        }

        private async Task<Tuple<Dictionary<string, double>,List<string>>> GetPriceTokensAsync(TradePairsGraph graph)
        {
            var withPriceTokens = new Dictionary<string, double>();
            var noPriceTokens = new List<string>();
            foreach (var relation in graph.Relations)
            {
                try
                {
                    var price = await GetPriceAsync(relation.Key);
                    withPriceTokens[relation.Key] = (double)price;
                }
                catch (Exception e)
                {
                    noPriceTokens.Add(relation.Key);
                }
            }

            return new Tuple<Dictionary<string, double>, List<string>>(withPriceTokens,noPriceTokens);
        }

        private async Task<TradePairsGraph> BuildRelationsAsync(List<TradePair> tradePairs)
        {
            var graph = new TradePairsGraph();
            foreach (var pair in tradePairs)
            {

                if (!graph.Relations.ContainsKey(pair.Token0.Symbol))
                {
                    graph.Relations[pair.Token0.Symbol] = new Dictionary<string, List<TradePair>>();
                }
                
                if (!graph.Relations.ContainsKey(pair.Token1.Symbol))
                {
                    graph.Relations[pair.Token1.Symbol] = new Dictionary<string, List<TradePair>>();
                }
                
                if (!graph.Relations[pair.Token0.Symbol].ContainsKey(pair.Token1.Symbol))
                {
                    graph.Relations[pair.Token0.Symbol][pair.Token1.Symbol] = new List<TradePair>();
                }
                
                if (!graph.Relations[pair.Token1.Symbol].ContainsKey(pair.Token0.Symbol))
                {
                    graph.Relations[pair.Token1.Symbol][pair.Token0.Symbol] = new List<TradePair>();
                }
                
                graph.Relations[pair.Token0.Symbol][pair.Token1.Symbol].Add(pair);
                graph.Relations[pair.Token1.Symbol][pair.Token0.Symbol].Add(pair);
            }

            return graph;
        }
        
        public async Task<TokenPricingMap> BuildPriceSpreadTrees(List<TradePair> tradePairs)
        {
            var graph = await BuildRelationsAsync(tradePairs);
            var priceTokens = await GetPriceTokensAsync(graph);
            
            Log.Information($"Price Spread. Build pricing map. " +
                                   $"Init: with price tokens: {JsonConvert.SerializeObject(priceTokens.Item1)}, " +
                                   $"no price tokens: {JsonConvert.SerializeObject(priceTokens.Item2)}");
            
            var tokensWithUSDPrice = priceTokens.Item1;
            var tokensNoPrice = priceTokens.Item2;
            var priceSpreadTrees = new List<PricingNode>();
            foreach (var token in tokensWithUSDPrice)
            {
                priceSpreadTrees.Add(new PricingNode
                {
                    FromTokenSymbol = null,
                    TokenSymbol = token.Key,
                    Depth = 0,
                    PriceInUsd = token.Value,
                    FromTradePairAddress = null,
                    ToTokens = new List<PricingNode>()
                });
            }

            // Iterate until all TokensNoPrice are priced
            int currentDepth = 1;
            while (tokensNoPrice.Count > 0)
            {
                List<PricingNode> currentLayer = priceSpreadTrees
                    .Where(node => node.Depth == currentDepth - 1)
                    .ToList();
                
                currentLayer.Sort((x, y) => 
                {
                    var xPriority = GetTokenPriority(x.TokenSymbol, tokensWithUSDPrice);
                    var yPriority = GetTokenPriority(y.TokenSymbol, tokensWithUSDPrice);

                    int result = xPriority.Item1.CompareTo(yPriority.Item1);
                    if (result != 0) return result;

                    return yPriority.Item2.CompareTo(xPriority.Item2); 
                });

                if (currentLayer.Count == 0)
                {
                    break;
                }
                
                foreach (var node in currentLayer)
                {
                    List<string> toTokens = tokensNoPrice.Where(to => CanPriceFrom(node.TokenSymbol, to, graph)).ToList();

                    foreach (var toToken in toTokens)
                    {
                        var tradePair = GetHighestPriorityTradePair(node.TokenSymbol, toToken, graph);
                        var price = (node.TokenSymbol == tradePair.Token0.Symbol
                            ? tradePair.ValueLocked0 / tradePair.ValueLocked1
                            : tradePair.ValueLocked1 / tradePair.ValueLocked0) * node.PriceInUsd;
                        var newNode = new PricingNode
                        {
                            PriceInUsd = price,
                            FromTokenSymbol = node.TokenSymbol,
                            TokenSymbol = toToken,
                            Depth = currentDepth,
                            FromTradePairAddress = tradePair?.Address,
                            FromTradePairId = tradePair.Id,
                            ToTokens = new List<PricingNode>()
                        };
                        node.ToTokens.Add(newNode);
                        priceSpreadTrees.Add(newNode);
                        
                        tokensNoPrice.Remove(toToken);
                        tokensWithUSDPrice[toToken] = price;
                    }
                }

                currentDepth++;
            }
            return new TokenPricingMap()
            {
                PriceSpreadTrees = priceSpreadTrees,
                TokenToPrice = tokensWithUSDPrice
            };
        }

        public async Task<List<TradePair>> GetTradePairAsync(string chainId)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<IndexTradePair>, QueryContainer>>();
            if (!string.IsNullOrEmpty(chainId))
            {
                mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
            }
            mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));
            QueryContainer Filter(QueryContainerDescriptor<IndexTradePair> f) => f.Bool(b => b.Must(mustQuery));

            var tradePairList = await _tradePairIndexRepository.GetListAsync(Filter);
            return tradePairList.Item2;
        }
        
        private async Task UpdateInternalPriceCacheAsync(Dictionary<string, double> needUpdateTokenPrice)
        {
            foreach (var tokenToPrice in needUpdateTokenPrice)
            {
                var key = $"{PriceOptions.InternalPriceCachePrefix}:{tokenToPrice.Key}";
                var priceDto = new PriceDto()
                {
                    PriceInUsd = (decimal)tokenToPrice.Value,
                    PriceUpdateTime = DateTime.UtcNow
                };
                await _internalPriceCache.SetAsync(key, priceDto, new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(PriceOptions.PriceSuperLongExpirationTime)
                });
                
                var time = DateTime.UtcNow.ToString("dd-MM-yyyy");
                var historyKey = $"{PriceOptions.InternalPriceHistoryCachePrefix}:{tokenToPrice.Key}:{time}";
                await _internalPriceCache.SetAsync(historyKey, priceDto, new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(PriceOptions.PriceSuperLongExpirationTime)
                });
                
                Log.Information($"Price Spread. Update affected from swap token price. " +
                                       $"key: {key}, historyKey: {historyKey}, price: {priceDto.PriceInUsd}, update time: {priceDto.PriceUpdateTime}");
            }
        }
        
        private async Task UpdaeNodePriceAsync(TokenPricingMap tokenPricingMap, PricingNode root, Dictionary<string, double> needUpdateTokenPrice)
        {
            var tradePair = await _tradePairIndexRepository.GetAsync(root.FromTradePairId);
            root.PriceInUsd = (root.FromTokenSymbol == tradePair.Token0.Symbol
                ? tradePair.ValueLocked0 / tradePair.ValueLocked1
                : tradePair.ValueLocked1 / tradePair.ValueLocked0) * tokenPricingMap.TokenToPrice[root.FromTokenSymbol];
            tokenPricingMap.TokenToPrice[root.TokenSymbol] = root.PriceInUsd;
            needUpdateTokenPrice[root.TokenSymbol] = root.PriceInUsd;
            Log.Information($"Price Spread. Update pricing map from trade pair. node: {root.TokenSymbol}, price: {root.PriceInUsd}");
            foreach (var node in root.ToTokens)
            {
                await UpdaeNodePriceAsync(tokenPricingMap, node, needUpdateTokenPrice);
            }
        }
        
        private async Task<bool> UpdateAffectedTokenPricesAsync(TokenPricingMap tokenPricingMap, PricingNode root, TradePair tradePair, double token0Amount, double token1Amount, Dictionary<string, double> needUpdateTokenPrice)
        {
            if (root.FromTradePairId != null && root.FromTradePairId == tradePair.Id)
            {
                root.PriceInUsd = (root.FromTokenSymbol == tradePair.Token0.Symbol
                    ? token0Amount / token1Amount
                    : token1Amount / token0Amount) * tokenPricingMap.TokenToPrice[root.FromTokenSymbol];
                tokenPricingMap.TokenToPrice[root.TokenSymbol] = root.PriceInUsd;
                needUpdateTokenPrice[root.TokenSymbol] = root.PriceInUsd;
                Log.Information($"Price Spread. Update pricing map from trade pair. Trade pair: {tradePair.Address}, spread from source: {root.TokenSymbol}, price: {root.PriceInUsd}");
                foreach (var node in root.ToTokens)
                {
                    await UpdaeNodePriceAsync(tokenPricingMap, node, needUpdateTokenPrice);
                }
                return true;
            }
           
            foreach (var node in root.ToTokens)
            {
                if (await UpdateAffectedTokenPricesAsync(tokenPricingMap, node, tradePair, token0Amount, token1Amount, needUpdateTokenPrice))
                {
                    return true;
                }
            }
            return false;
        }
        
        public async Task RebuildPricingMapAsync(string chainId)
        {
            var tradePairList = await GetTradePairAsync(chainId);
            var tokenPricingMap = await BuildPriceSpreadTrees(tradePairList);
            foreach (var root in tokenPricingMap.PriceSpreadTrees)
            {
                if (root.Depth > 0)
                {
                    continue;
                }
                PrintPricingNodeTree(root, 0);
            }

            if (tokenPricingMap.PriceSpreadTrees.Count > 0)
            {
                var pricingMapKey = $"{PriceOptions.PricingMapCachePrefix}:{chainId}";
                await using var handle = await _distributedLock.TryAcquireAsync(pricingMapKey, TimeSpan.FromSeconds(PriceOptions.CacheLockTimeoutSeconds));
                if (handle != null)
                {
                    await _tokenPricingMapCache.SetAsync(pricingMapKey, tokenPricingMap);
                    await UpdateInternalPriceCacheAsync(tokenPricingMap.TokenToPrice);
                    Log.Information($"Price Spread. Rebuild pricing tree. Cache key: {pricingMapKey}, price count: {tokenPricingMap.TokenToPrice.Count}");
                }
                else
                {
                    Log.Information($"Price Spread. Rebuild pricing tree failed, can not get cache lock {pricingMapKey}");
                }
            }
        }
        
        [ExceptionHandler(typeof(Exception), 
            LogLevel = LogLevel.Error, TargetType = typeof(HandlerExceptionService), MethodName = nameof(HandlerExceptionService.HandleWithReturn))]
        public virtual async Task UpdateAffectedPriceMapAsync(string chainId, Guid tradePairId, string token0Amount, string token1Amount)
        {
            var pricingMapKey = $"{PriceOptions.PricingMapCachePrefix}:{chainId}";
            await using var handle = await _distributedLock.TryAcquireAsync(pricingMapKey, TimeSpan.FromSeconds(PriceOptions.CacheLockTimeoutSeconds));
            if (handle != null)
            {
                var tokenPricingMap = await _tokenPricingMapCache.GetAsync(pricingMapKey);
                bool newTree = false;
                if (tokenPricingMap == null || tokenPricingMap.PriceSpreadTrees == null)
                {
                    newTree = true;
                    var tradePairList = await GetTradePairAsync(chainId);
                    tokenPricingMap = await BuildPriceSpreadTrees(tradePairList);
                    foreach (var root in tokenPricingMap.PriceSpreadTrees)
                    {
                        if (root.Depth > 0)
                        {
                            continue;
                        }
                        PrintPricingNodeTree(root, 0);
                    }
                }
                
                var pair = await _tradePairIndexRepository.GetAsync(tradePairId);
                if (pair == null)
                {
                    Log.Information($"Price Spread. Get pair: {tradePairId} failed");
                    return;
                }

                Dictionary<string, double> needUpdateTokenPrice = new Dictionary<string, double>();
                foreach (var root in tokenPricingMap.PriceSpreadTrees)
                {
                    await UpdateAffectedTokenPricesAsync(tokenPricingMap, root, pair, double.Parse(token0Amount), double.Parse(token1Amount), needUpdateTokenPrice);
                }
                await _tokenPricingMapCache.SetAsync(pricingMapKey, tokenPricingMap);
                if (newTree)
                {
                    await UpdateInternalPriceCacheAsync(tokenPricingMap.TokenToPrice);
                    Log.Information($"Price Spread. Build new tree and cache price. Cache key: {pricingMapKey}, update price count: {tokenPricingMap.TokenToPrice.Count}");
                }
                else
                {
                    await UpdateInternalPriceCacheAsync(needUpdateTokenPrice);
                    Log.Information($"Price Spread. Update tree and cache price. Cache key: {pricingMapKey}, need update price count: {needUpdateTokenPrice.Count}");
                }
            }
            else
            {
                 Log.Information($"Price Spread. Update tree and cache price failed, can not get cache lock: {pricingMapKey}");
            }
        }
        
    }
    
    
    public class PriceDto
    {
        public decimal PriceInUsd { get; set; } = PriceOptions.DefaultPriceValue;
        public DateTime PriceUpdateTime { get; set; }
        
    }
    
    public class TokenPricingMap
    {
        public List<PricingNode> PriceSpreadTrees { get; set; }
        public Dictionary<string, double> TokenToPrice { get; set; }
    }
    
    public class PricingNode
    {
        public double PriceInUsd { get; set; } 
        public int Depth { get; set; }
        public string FromTokenSymbol { get; set; }
        public string FromTradePairAddress { get; set; }
        public Guid FromTradePairId { get; set; }
        public string TokenSymbol { get; set; }
        public List<PricingNode> ToTokens { get; set; }
    }

    public class TradePairsGraph
    {
        public Dictionary<string, Dictionary<string, List<TradePair>>> Relations { get; set; } = new();
    }
}