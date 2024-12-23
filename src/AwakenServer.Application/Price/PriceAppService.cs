using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Price.Dtos;
using AwakenServer.Tokens.Dtos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Serilog;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Volo.Abp.DistributedLocking;
using IndexTradePair = AwakenServer.Trade.Index.TradePair;
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
        private readonly ILogger _logger;
        private readonly IAbpDistributedLock _distributedLock;
        private readonly IDistributedCache<string> _priceExceptionCache;
        public PriceAppService(IDistributedCache<PriceDto> priceCache,
            IDistributedCache<PriceDto> internalPriceCache,
            ITokenPriceProvider tokenPriceProvider,
            IOptionsSnapshot<TokenPriceOptions> options,
            INESTRepository<IndexTradePair, Guid> tradePairIndexRepository,
            IDistributedCache<TokenPricingMap> tokenPricingMap,
            IAbpDistributedLock distributedLock,
            IDistributedCache<string> priceExceptionCache)
        {
            _priceCache = priceCache;
            _tokenPriceProvider = tokenPriceProvider;
            _tokenPriceOptions = options;
            _tradePairIndexRepository = tradePairIndexRepository;
            _logger = Log.ForContext<PriceAppService>();
            _tokenPricingMapCache = tokenPricingMap;
            _internalPriceCache = internalPriceCache;
            _distributedLock = distributedLock;
            _priceExceptionCache = priceExceptionCache;
        }

        public async Task<string> GetTokenPriceAsync(GetTokenPriceInput input)
        {
            if (string.IsNullOrWhiteSpace(input.Symbol)) return "0";
            var result = await GetTokenPriceListAsync(new List<string>{ input.Symbol });
            if (result.Items.Count == 0) return "0";
            else return result.Items[0].PriceInUsd.ToString();
        }

        public async Task<double> GetTokenPriceAsync(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return 0;
            var result = await GetTokenPriceListAsync(new List<string>{ symbol });
            if (result.Items.Count == 0) return 0;
            return (double)result.Items[0].PriceInUsd;
        }
        
        private async Task<decimal> GetApiUsdtPriceAsync(string time)
        {
            if (String.IsNullOrEmpty(time))
            {
                return await _tokenPriceProvider.GetPriceAsync(PriceOptions.UsdtPricePair);
            }
           
            return await _tokenPriceProvider.GetHistoryPriceAsync(PriceOptions.UsdtPricePair, time);
        }

        [ExceptionHandler(typeof(Exception), Message = "GetTokenApi Error",
            LogLevel = LogLevel.Error, TargetType = typeof(HandlerExceptionService), MethodName = nameof(HandlerExceptionService.HandleWithReturnNull))]
        public virtual string GetTokenApiName(string symbol)
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
        
        [ExceptionHandler(typeof(Exception), ReturnDefault = ReturnDefault.Default)]
        public virtual async Task<decimal> GetPriceAsync(string symbol)
        {
            var pair = GetTokenApiName(symbol);
            if (String.IsNullOrEmpty(pair))
            {
                _logger.Error($"Get token price symbol: {symbol}, nonexistent mapping result price: 0");
                return 0;
            }
            
            var rawPrice = await _tokenPriceProvider.GetPriceAsync(pair);
            var result = await ProcessTokenPrice(symbol, rawPrice, null);
            _logger.Information($"Get token price symbol: {symbol}, pair: {pair}, rawPrice: {rawPrice}, result price: {result}");
            return result;
        }

        [ExceptionHandler(typeof(Exception), Message = "GetHistoryPrice Error", ReturnDefault = ReturnDefault.Default)]
        public virtual async Task<decimal> GetHistoryPriceAsync(string symbol, string time)
        {
            var pair = GetTokenApiName(symbol);
            if (String.IsNullOrEmpty(pair))
            {
                _logger.Information($"Get history token price symbol: {symbol}, nonexistent mapping result price: 0");
                return 0;
            }
            
            var rawPrice = await _tokenPriceProvider.GetHistoryPriceAsync(pair, time);
            var result = await ProcessTokenPrice(symbol, rawPrice, time);
            
            _logger.Information($"Get history token price symbol: {symbol}, pair: {pair}, time: {time}, rawPrice: {rawPrice}, result price: {result}");
            
            return result;
        }

        private async Task<bool> IsNeedFetchPriceAsync(string symbol, PriceDto priceDto)
        {
            var exceptionKey = $"{PriceOptions.PriceExceptionCachePrefix}:{symbol}";
            var existed = await _priceExceptionCache.GetAsync(exceptionKey);
            if (!existed.IsNullOrWhiteSpace())
            {
                return false;
            }
            
            return priceDto.PriceInUsd == 0 || 
                   priceDto.PriceInUsd == PriceOptions.DefaultPriceValue ||
                   priceDto.PriceUpdateTime.AddSeconds(_tokenPriceOptions.Value.PriceExpirationTimeSeconds) <= DateTime.UtcNow;
        }
        
        private async Task<bool> IsHistoryDataNeedFetchPriceAsync(string symbol, PriceDto priceDto)
        {
            var exceptionKey = $"{PriceOptions.PriceExceptionCachePrefix}:{symbol}";
            var existed = await _priceExceptionCache.GetAsync(exceptionKey);
            if (!existed.IsNullOrWhiteSpace())
            {
                return false;
            }
            
            return priceDto.PriceInUsd == 0 || 
                   priceDto.PriceInUsd == PriceOptions.DefaultPriceValue;
        }
        
        
        public async Task<TokenPriceDataDto> GetApiTokenPriceAsync(string symbol)
        {
            var key = $"{PriceOptions.PriceCachePrefix}:{symbol}";
            var price = await _priceCache.GetOrAddAsync(key, async () => new PriceDto());
            
            if (await IsNeedFetchPriceAsync(symbol, price))
            {
                try
                {
                    var priceInUsd = await GetPriceAsync(symbol);
                    if (priceInUsd > 0)
                    {
                        price.PriceInUsd = priceInUsd;
                        price.PriceUpdateTime = DateTime.UtcNow;
                        await _priceCache.SetAsync(key, price, new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(PriceOptions.PriceSuperLongExpirationTime)
                        });
                    }
                }
                catch (Exception e)
                {
                    var exceptionKey = $"{PriceOptions.PriceExceptionCachePrefix}:{symbol}";
                    _priceExceptionCache.Set(exceptionKey, "1", new DistributedCacheEntryOptions
                    {
                        AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(PriceOptions.ExceptionCacheMinutes)
                    });
                    _logger.Error(e, $"Get token price symbol: {symbol} failed.");
                }
                
            }
                    
            _logger.Information($"Get token price symbol: {symbol}, return price: {price.PriceInUsd}");
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
                _logger.Information($"Get internal token price symbol: {symbol}, return price: {internalPriceDto.PriceInUsd}");
                return new TokenPriceDataDto()
                {
                    Symbol = symbol,
                    PriceInUsd = internalPriceDto.PriceInUsd
                };
            }
            
            _logger.Information($"Get internal token price symbol: {symbol}, return price: 0");
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
            var historySymbol = $"{input.Symbol}-{time}";
            
            if (await IsHistoryDataNeedFetchPriceAsync(historySymbol, price))
            {
                try
                {
                    price.PriceInUsd = await GetHistoryPriceAsync(input.Symbol, time);
                    price.PriceUpdateTime = DateTime.UtcNow;
                    await _priceCache.SetAsync(key, price, new DistributedCacheEntryOptions
                    {
                        AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(PriceOptions.PriceSuperLongExpirationTime)
                    });
                }
                catch (Exception e)
                {
                    var exceptionKey = $"{PriceOptions.PriceExceptionCachePrefix}:{historySymbol}";
                    _priceExceptionCache.Set(exceptionKey, "1", new DistributedCacheEntryOptions
                    {
                        AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(PriceOptions.ExceptionCacheMinutes)
                    });
                    _logger.Error(e, $"Get history token price symbol: {historySymbol} failed.");
                }
            }

            if (price.PriceInUsd == 0 || price.PriceInUsd == PriceOptions.DefaultPriceValue)
            {
                var toleranceDay = 0;
                var beforeTime = input.DateTime;
                while (toleranceDay < PriceOptions.HistoryPriceToleranceDays)
                {
                    toleranceDay++;
                    beforeTime = beforeTime.AddDays(-1);
                    var beforeKey = $"{PriceOptions.PriceHistoryCachePrefix}:{input.Symbol}:{beforeTime.ToString("dd-MM-yyyy")}";
                    var beforePrice = await _priceCache.GetOrAddAsync(beforeKey, async () => new PriceDto());
                    if (beforePrice.PriceInUsd > 0)
                    {
                        price = beforePrice;
                        break;
                    }
                    _logger.Information($"Get history token price look before, toleranceDay: {toleranceDay}, beforeTime: {beforeTime}, key: {beforeKey}");
                }
            }
            
            _logger.Information($"Get history token price symbol: {input.Symbol}, time: {input.DateTime}, priceTime: {price.PriceUpdateTime}, return price: {price.PriceInUsd}");
            
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
                _logger.Information($"Get internal history token price symbol: {input.Symbol}, return price: {internalPriceDto.PriceInUsd}");
                return new TokenPriceDataDto()
                {
                    Symbol = input.Symbol,
                    PriceInUsd = internalPriceDto.PriceInUsd
                };
            }
            
            
            _logger.Information($"Get internal history token price symbol: {input.Symbol}, return price: 0");
            return new TokenPriceDataDto()
            {
                Symbol = input.Symbol,
                PriceInUsd = 0
            };
        }
        
        [ExceptionHandler(typeof(Exception), Message = "GetTokenPriceList Error", ReturnDefault = ReturnDefault.New)]
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

        [ExceptionHandler(typeof(Exception), Message = "GetTokenHistoryPriceData Error",
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
        
        [ExceptionHandler(typeof(Exception), Message = "GetTokenHistoryPriceData Error",
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
            _logger.Information($"{new string(' ', indent * 2)}Price Spread. Tree Depth: {node.Depth}, FromTokenSymbol: {node.FromTokenSymbol}, TokenSymbol: {node.TokenSymbol}, PriceInUsd: {node.PriceInUsd}, FromTradePairAddress: {node.FromTradePairAddress}");

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

        private IndexTradePair GetHighestPriorityTradePair(string from, string to, TradePairsGraph graph)
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
                    if (price > 0)
                    {
                        withPriceTokens[relation.Key] = (double) price;
                    }
                    else
                    {
                        noPriceTokens.Add(relation.Key);
                    }
                }
                catch (Exception e)
                {
                    noPriceTokens.Add(relation.Key);
                }
                
            }

            return new Tuple<Dictionary<string, double>, List<string>>(withPriceTokens,noPriceTokens);
        }

        private async Task<TradePairsGraph> BuildRelationsAsync(List<IndexTradePair> tradePairs)
        {
            var graph = new TradePairsGraph();
            foreach (var pair in tradePairs)
            {

                if (!graph.Relations.ContainsKey(pair.Token0.Symbol))
                {
                    graph.Relations[pair.Token0.Symbol] = new Dictionary<string, List<IndexTradePair>>();
                }
                
                if (!graph.Relations.ContainsKey(pair.Token1.Symbol))
                {
                    graph.Relations[pair.Token1.Symbol] = new Dictionary<string, List<IndexTradePair>>();
                }
                
                if (!graph.Relations[pair.Token0.Symbol].ContainsKey(pair.Token1.Symbol))
                {
                    graph.Relations[pair.Token0.Symbol][pair.Token1.Symbol] = new List<IndexTradePair>();
                }
                
                if (!graph.Relations[pair.Token1.Symbol].ContainsKey(pair.Token0.Symbol))
                {
                    graph.Relations[pair.Token1.Symbol][pair.Token0.Symbol] = new List<IndexTradePair>();
                }
                
                graph.Relations[pair.Token0.Symbol][pair.Token1.Symbol].Add(pair);
                graph.Relations[pair.Token1.Symbol][pair.Token0.Symbol].Add(pair);
            }

            return graph;
        }
        
        public async Task<TokenPricingMap> BuildPriceSpreadTrees(List<IndexTradePair> tradePairs)
        {
            var graph = await BuildRelationsAsync(tradePairs);
            var priceTokens = await GetPriceTokensAsync(graph);
            
            _logger.Information($"Price Spread. Build pricing map. " +
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

        public async Task<List<IndexTradePair>> GetTradePairAsync(string chainId)
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
                
                _logger.Information($"Price Spread. Update affected from swap token price. " +
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
            _logger.Information($"Price Spread. Update pricing map from trade pair. node: {root.TokenSymbol}, price: {root.PriceInUsd}");
            foreach (var node in root.ToTokens)
            {
                await UpdaeNodePriceAsync(tokenPricingMap, node, needUpdateTokenPrice);
            }
        }
        
        private async Task<bool> UpdateAffectedTokenPricesAsync(TokenPricingMap tokenPricingMap, PricingNode root, IndexTradePair tradePair, double token0Amount, double token1Amount, Dictionary<string, double> needUpdateTokenPrice)
        {
            if (root.FromTradePairId != null && root.FromTradePairId == tradePair.Id)
            {
                root.PriceInUsd = (root.FromTokenSymbol == tradePair.Token0.Symbol
                    ? token0Amount / token1Amount
                    : token1Amount / token0Amount) * tokenPricingMap.TokenToPrice[root.FromTokenSymbol];
                tokenPricingMap.TokenToPrice[root.TokenSymbol] = root.PriceInUsd;
                needUpdateTokenPrice[root.TokenSymbol] = root.PriceInUsd;
                _logger.Information($"Price Spread. Update pricing map from trade pair. Trade pair: {tradePair.Address}, spread from source: {root.TokenSymbol}, price: {root.PriceInUsd}");
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
                    _logger.Information($"Price Spread. Rebuild pricing tree. Cache key: {pricingMapKey}, price count: {tokenPricingMap.TokenToPrice.Count}");
                }
                else
                {
                    _logger.Information($"Price Spread. Rebuild pricing tree failed, can not get cache lock {pricingMapKey}");
                }
            }
        }
        
        [ExceptionHandler(typeof(Exception), Message = "UpdateAffectedPrice Error", TargetType = typeof(HandlerExceptionService), MethodName = nameof(HandlerExceptionService.HandleWithReturn))]
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
                    _logger.Information($"Price Spread. Get pair: {tradePairId} failed");
                    return;
                }

                var needUpdateTokenPrice = new Dictionary<string, double>();
                foreach (var root in tokenPricingMap.PriceSpreadTrees)
                {
                    await UpdateAffectedTokenPricesAsync(tokenPricingMap, root, pair, double.Parse(token0Amount), double.Parse(token1Amount), needUpdateTokenPrice);
                }
                await _tokenPricingMapCache.SetAsync(pricingMapKey, tokenPricingMap);
                if (newTree)
                {
                    await UpdateInternalPriceCacheAsync(tokenPricingMap.TokenToPrice);
                    _logger.Information($"Price Spread. Build new tree and cache price. Cache key: {pricingMapKey}, update price count: {tokenPricingMap.TokenToPrice.Count}");
                }
                else
                {
                    await UpdateInternalPriceCacheAsync(needUpdateTokenPrice);
                    _logger.Information($"Price Spread. Update tree and cache price. Cache key: {pricingMapKey}, need update price count: {needUpdateTokenPrice.Count}");
                }
            }
            else
            {
                 _logger.Information($"Price Spread. Update tree and cache price failed, can not get cache lock: {pricingMapKey}");
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
        public Dictionary<string, Dictionary<string, List<IndexTradePair>>> Relations { get; set; } = new();
    }
}