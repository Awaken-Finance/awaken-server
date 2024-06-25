using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Index = System.Index;
using IndexTradePair = AwakenServer.Trade.Index.TradePair;
using JsonConvert = Newtonsoft.Json.JsonConvert;

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

        public PriceAppService(IDistributedCache<PriceDto> priceCache,
            IDistributedCache<PriceDto> internalPriceCache,
            ITokenPriceProvider tokenPriceProvider,
            IOptionsSnapshot<TokenPriceOptions> options,
            INESTRepository<IndexTradePair, Guid> tradePairIndexRepository,
            ILogger<PriceAppService> logger,
            IDistributedCache<TokenPricingMap> tokenPricingMap)
        {
            _priceCache = priceCache;
            _tokenPriceProvider = tokenPriceProvider;
            _tokenPriceOptions = options;
            _tradePairIndexRepository = tradePairIndexRepository;
            _logger = logger;
            _tokenPricingMapCache = tokenPricingMap;
            _internalPriceCache = internalPriceCache;
        }

        public async Task<string> GetApiTokenPriceAsync(GetTokenPriceInput input)
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

        private string GetTokenApiName(string symbol)
        {
            if (String.IsNullOrEmpty(symbol))
            {
                return null;
            }

            try
            {
                _tokenPriceOptions.Value.PriceTokenMapping.TryGetValue(symbol.ToUpper(), out var priceTradePair);
                return priceTradePair;
            }
            catch (Exception e)
            {
                Logger.LogInformation($"Get token price symbol: {symbol}, nonexistent mapping symbol");
                return null;
            }
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
            Logger.LogInformation($"Get token price symbol: {symbol}, pair: {pair}, rawPrice: {rawPrice}, result price: {result}");
            return result;
        }

        private async Task<decimal> GetHistoryPriceAsync(string symbol, string time)
        {
            var pair = GetTokenApiName(symbol);
            if (String.IsNullOrEmpty(pair))
            {
                Logger.LogInformation($"Get history token price symbol: {symbol}, nonexistent mapping result price: 0");
                return 0;
            }
            
            var rawPrice = await _tokenPriceProvider.GetHistoryPriceAsync(pair, time);
            var result = await ProcessTokenPrice(symbol, rawPrice, time);
            
            Logger.LogInformation($"Get history token price symbol: {symbol}, pair: {pair}, time: {time}, rawPrice: {rawPrice}, result price: {result}");
            
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
                    await _priceCache.SetAsync(key, price);
                }
                catch (Exception e)
                {
                    // TODO: Remove this code in the next version (v2.2)
                    // This code is temporarily added to fix historical data issues.
                    if (price.PriceUpdateTime == DateTime.MinValue)
                    {
                        price.PriceUpdateTime = DateTime.UtcNow.AddHours(-1);
                        await _priceCache.SetAsync(key, price);
                    }
                    if (price.PriceInUsd == PriceOptions.DefaultPriceValue)
                    {
                        price.PriceInUsd = 0;
                        await _priceCache.SetAsync(key, price);
                    }
                    Logger.LogError(e, $"Get token price symbol: {symbol} failed. Return old data price: {price.PriceInUsd}");
                }
            }
                    
            Logger.LogInformation($"Get token price symbol: {symbol}, return price: {price.PriceInUsd}");
            return new TokenPriceDataDto
            {
                Symbol = symbol,
                PriceInUsd = price.PriceInUsd
            };
        }

        public async Task<Tuple<TokenPriceDataDto, TokenPriceDataDto>> GetPairTokenPriceAsync(string chainId, Guid tradePairId, string symbol0,
            string symbol1)
        {
            var token0PricePair = GetTokenApiName(symbol0);
            var token1PricePair = GetTokenApiName(symbol1);
            if (!string.IsNullOrEmpty(token0PricePair) && !string.IsNullOrEmpty(token1PricePair))
            {
                var token0PriceDto = await GetApiTokenPriceAsync(symbol0);
                var token1PriceDto = await GetApiTokenPriceAsync(symbol1);
                return new Tuple<TokenPriceDataDto, TokenPriceDataDto>(token0PriceDto, token1PriceDto);
            }
            else if (!string.IsNullOrEmpty(token1PricePair))
            {
                var dependsTokenPriceDto = await GetApiTokenPriceAsync(symbol1);
                var tradePair = await _tradePairIndexRepository.GetAsync(tradePairId);
                var price = (decimal)tradePair.ValueLocked1 / (decimal)tradePair.ValueLocked0 * dependsTokenPriceDto.PriceInUsd;
                return new Tuple<TokenPriceDataDto, TokenPriceDataDto>(new TokenPriceDataDto()
                {
                    Symbol = symbol0,
                    PriceInUsd = price
                }, dependsTokenPriceDto);
            }
            else
            {
                TokenPriceDataDto dependsTokenPriceDto;
                if (!string.IsNullOrEmpty(token0PricePair))
                {
                    dependsTokenPriceDto = await GetApiTokenPriceAsync(symbol0);
                }
                else
                {
                    dependsTokenPriceDto = await GetInternalTokenPriceAsync(symbol0);
                }
                var tradePair = await _tradePairIndexRepository.GetAsync(tradePairId);
                var price = (decimal)tradePair.ValueLocked0 / (decimal)tradePair.ValueLocked1 * dependsTokenPriceDto.PriceInUsd;
                return new Tuple<TokenPriceDataDto, TokenPriceDataDto>(dependsTokenPriceDto, new TokenPriceDataDto()
                {
                    Symbol = symbol1,
                    PriceInUsd = price
                });
            }
        }

        private async Task<TokenPriceDataDto> GetInternalTokenPriceAsync(string symbol)
        {
            var key = $"{PriceOptions.InternalPriceCachePrefix}:{symbol}";
            var internalPriceDto = await _internalPriceCache.GetAsync(key);
            if (internalPriceDto != null)
            {
                return new TokenPriceDataDto()
                {
                    Symbol = symbol,
                    PriceInUsd = internalPriceDto.PriceInUsd
                };
            }
          
            return new TokenPriceDataDto()
            {
                Symbol = symbol,
                PriceInUsd = 0
            };
        }
        
        public async Task<ListResultDto<TokenPriceDataDto>> GetTokenPriceListAsync(List<string> symbols)
        {
            var result = new List<TokenPriceDataDto>();
            if (symbols.Count == 0)
            {
                return new ListResultDto<TokenPriceDataDto>();
            }

            try
            {
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
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Get token price failed.");
                throw;
            }

            return new ListResultDto<TokenPriceDataDto>
            {
                Items = result
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
                try
                {
                    price.PriceInUsd = await GetHistoryPriceAsync(input.Symbol, time);
                    price.PriceUpdateTime = DateTime.UtcNow;
                    await _priceCache.SetAsync(key, price);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, $"Get history token price symbol: {input.Symbol}, time: {time} failed. Return old data price: {price.PriceInUsd}");
                }
                       
            }
                    
            Logger.LogInformation($"Get history token price symbol: {input.Symbol}, time: {time}, return price: {price.PriceInUsd}");
            
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
                return new TokenPriceDataDto()
                {
                    Symbol = input.Symbol,
                    PriceInUsd = internalPriceDto.PriceInUsd
                };
            }
          
            return new TokenPriceDataDto()
            {
                Symbol = input.Symbol,
                PriceInUsd = 0
            };
        }
        
        public async Task<ListResultDto<TokenPriceDataDto>> GetTokenHistoryPriceDataAsync(
            List<GetTokenHistoryPriceInput> inputs)
        {
            var result = new List<TokenPriceDataDto>();
            try
            {
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
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Get history token price failed.");
                throw;
            }

            return new ListResultDto<TokenPriceDataDto>
            {
                Items = result
            };
        }
        
        public void PrintPricingNodeTree(PricingNode node, int indent = 0)
        {
            _logger.LogInformation($"{new string(' ', indent * 2)}Depth: {node.Depth}, FromTokenSymbol: {node.FromTokenSymbol}, TokenSymbol: {node.TokenSymbol}, PriceInUsd: {node.PriceInUsd}, FromTradePairAddress: {node.FromTradePairAddress}");

            foreach (var child in node.ToTokens)
            {
                PrintPricingNodeTree(child, indent + 1);
            }
        }
        
        private int GetTokenPriority(string token)
        {
            var priority = _tokenPriceOptions.Value.StablecoinPriority.IndexOf(token);
            return priority >= 0 ? priority : int.MaxValue;
        }
        
        private bool CanPriceFrom(string from, string to, TradePairsGraph graph)
        {
            return graph.Relations.ContainsKey(from) && graph.Relations[from].ContainsKey(to);
        }

        private TradePair GetHighestPriorityTradePairAddress(string from, string to, TradePairsGraph graph)
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
            
            _logger.LogInformation($"Build pricing map. " +
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
                List<PricingNode> currentLayer = priceSpreadTrees.Where(node => node.Depth == currentDepth - 1)
                    .OrderBy(node => GetTokenPriority(node.FromTokenSymbol))
                    .ToList();

                if (currentLayer.Count == 0)
                {
                    break;
                }
                
                foreach (var node in currentLayer)
                {
                    List<string> toTokens = tokensNoPrice.Where(to => CanPriceFrom(node.TokenSymbol, to, graph)).ToList();

                    foreach (var toToken in toTokens)
                    {
                        var tradePair = GetHighestPriorityTradePairAddress(node.TokenSymbol, toToken, graph);
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
        
        private async Task UpdateMapPrice(string chainId, TokenPricingMap tokenPricingMap)
        {
            foreach (var tokenToPrice in tokenPricingMap.TokenToPrice)
            {
                var key = $"{PriceOptions.InternalPriceCachePrefix}:{tokenToPrice.Key}";
                var priceDto = new PriceDto()
                {
                    PriceInUsd = (decimal)tokenToPrice.Value,
                    PriceUpdateTime = DateTime.UtcNow
                };
                await _internalPriceCache.SetAsync(key, priceDto);
                
                var time = DateTime.UtcNow.ToString("dd-MM-yyyy");
                var historyKey = $"{PriceOptions.InternalPriceHistoryCachePrefix}:{tokenToPrice.Key}:{time}";
                await _internalPriceCache.SetAsync(historyKey, priceDto);
                
                _logger.LogInformation($"Update pricing map. " +
                                       $"Flush result to cache: key: {key}, price: {priceDto.PriceInUsd}, update time: {priceDto.PriceUpdateTime}");
            }
            var pricingMapKey = $"{PriceOptions.PricingMapCachePrefix}:{chainId}";
            await _tokenPricingMapCache.SetAsync(pricingMapKey, tokenPricingMap);
        }
        
        private async Task UpdaeNodePriceAsync(TokenPricingMap tokenPricingMap, PricingNode root)
        {
            var tradePair = await _tradePairIndexRepository.GetAsync(root.FromTradePairId);
            root.PriceInUsd = (root.FromTokenSymbol == tradePair.Token0.Symbol
                ? tradePair.ValueLocked0 / tradePair.ValueLocked1
                : tradePair.ValueLocked1 / tradePair.ValueLocked0) * tokenPricingMap.TokenToPrice[root.FromTokenSymbol];
            tokenPricingMap.TokenToPrice[root.TokenSymbol] = root.PriceInUsd;
            _logger.LogInformation($"Update pricing map from trade pair. node: {root.TokenSymbol}, price: {root.PriceInUsd}");
            foreach (var node in root.ToTokens)
            {
                await UpdaeNodePriceAsync(tokenPricingMap, node);
            }
        }
        
        private async Task<bool> UpdateAffectedTokenPricesAsync(TokenPricingMap tokenPricingMap, PricingNode root, TradePair tradePair, double token0Amount, double token1Amount)
        {
            if (root.FromTradePairId != null && root.FromTradePairId == tradePair.Id)
            {
                root.PriceInUsd = (root.FromTokenSymbol == tradePair.Token0.Symbol
                    ? token0Amount / token1Amount
                    : token1Amount / token0Amount) * tokenPricingMap.TokenToPrice[root.FromTokenSymbol];
                tokenPricingMap.TokenToPrice[root.TokenSymbol] = root.PriceInUsd;
                
                _logger.LogInformation($"Update pricing map from trade pair. Trade pair: {tradePair.Address}-{tradePair.Id}, spread from source: {root.TokenSymbol}, price: {root.PriceInUsd}");
                
                foreach (var node in root.ToTokens)
                {
                    await UpdaeNodePriceAsync(tokenPricingMap, node);
                }
                return true;
            }
           
            foreach (var node in root.ToTokens)
            {
                if (await UpdateAffectedTokenPricesAsync(tokenPricingMap, node, tradePair, token0Amount, token1Amount))
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
            await UpdateMapPrice(chainId, tokenPricingMap);
        }
        
        public async Task UpdateAffectedPriceMapAsync(string chainId, Guid tradePairId, string token0Amount, string token1Amount)
        {
            var pricingMapKey = $"{PriceOptions.PricingMapCachePrefix}:{chainId}";
            var tokenPricingMap = await _tokenPricingMapCache.GetAsync(pricingMapKey);
            if (tokenPricingMap.PriceSpreadTrees != null)
            {
                var pair = await _tradePairIndexRepository.GetAsync(tradePairId);
                foreach (var root in tokenPricingMap.PriceSpreadTrees)
                {
                    await UpdateAffectedTokenPricesAsync(tokenPricingMap, root, pair, double.Parse(token0Amount), double.Parse(token1Amount));
                }
                await UpdateMapPrice(chainId, tokenPricingMap);
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