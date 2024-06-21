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

namespace AwakenServer.Price
{
    [RemoteService(IsEnabled = false)]
    public class PriceAppService : ApplicationService, IPriceAppService
    {
        private readonly IDistributedCache<PriceDto> _priceCache;
        private readonly ITokenPriceProvider _tokenPriceProvider;
        private readonly IOptionsSnapshot<TokenPriceOptions> _tokenPriceOptions;
        private readonly INESTRepository<IndexTradePair, Guid> _tradePairIndexRepository;
        private readonly IDistributedCache<TokenPricingMap> _tokenPricingMap;
        
        public PriceAppService(IDistributedCache<PriceDto> priceCache,
            ITokenPriceProvider tokenPriceProvider,
            IOptionsSnapshot<TokenPriceOptions> options,
            INESTRepository<IndexTradePair, Guid> tradePairIndexRepository)
        {
            _priceCache = priceCache;
            _tokenPriceProvider = tokenPriceProvider;
            _tokenPriceOptions = options;
            _tradePairIndexRepository = tradePairIndexRepository;
        }

        public async Task<string> GetTokenPriceAsync(GetTokenPriceInput input)
        {
            if (string.IsNullOrWhiteSpace(input.Symbol)) return "0";
            var result = await GetTokenPriceListAsync(new List<string>{ input.Symbol });
            if (result.Items.Count == 0) return "0";
            else return result.Items[0].PriceInUsd.ToString();
        }

        private async Task<decimal> GetUsdtPriceAsync(string time)
        {
            if (String.IsNullOrEmpty(time))
            {
                return await _tokenPriceProvider.GetPriceAsync(PriceOptions.UsdtPricePair);
            }
           
            return await _tokenPriceProvider.GetHistoryPriceAsync(PriceOptions.UsdtPricePair, time);
        }

        private string GetPriceTradePair(string symbol)
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
                var usdtPrice = await GetUsdtPriceAsync(time);
                return rawPrice * usdtPrice;
            }

            return rawPrice;
        }
        
        
        private async Task<decimal> GetPriceAsync(string symbol)
        {
            var pair = GetPriceTradePair(symbol);
            if (String.IsNullOrEmpty(pair))
            {
                Logger.LogInformation($"Get token price symbol: {symbol}, nonexistent mapping result price: 0");
                return 0;
            }
            
            var rawPrice = await _tokenPriceProvider.GetPriceAsync(pair);
            var result = await ProcessTokenPrice(symbol, rawPrice, null);
            
            Logger.LogInformation($"Get token price symbol: {symbol}, pair: {pair}, rawPrice: {rawPrice}, result price: {result}");
            
            return result;
        }

        private async Task<decimal> GetHistoryPriceAsync(string symbol, string time)
        {
            var pair = GetPriceTradePair(symbol);
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
                    var key = $"{PriceOptions.PriceCachePrefix}:{symbolList[i]}";
                    var price = await _priceCache.GetOrAddAsync(key, async () => new PriceDto());
                    
                    if (IsNeedFetchPrice(price))
                    {
                        try
                        {
                            price.PriceInUsd = await GetPriceAsync(symbolList[i]);
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
                            Logger.LogError(e, $"Get token price symbol: {symbolList[i]} failed. Return old data price: {price.PriceInUsd}");
                        }
                    }
                    
                    Logger.LogInformation($"Get token price symbol: {symbolList[i]}, return price: {price.PriceInUsd}");
                    
                    result.Add(new TokenPriceDataDto
                    {
                        Symbol = symbolList[i],
                        PriceInUsd = price.PriceInUsd
                    });
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

        public async Task<ListResultDto<TokenPriceDataDto>> GetTokenHistoryPriceDataAsync(
            List<GetTokenHistoryPriceInput> inputs)
        {
            var result = new List<TokenPriceDataDto>();
            try
            {
                foreach (var input in inputs)
                {
                    var time = input.DateTime.ToString("dd-MM-yyyy");
                    if (input.Symbol.IsNullOrEmpty())
                    {
                        result.Add(new TokenPriceDataDto());
                        continue;
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
                    
                    result.Add(new TokenPriceDataDto
                    {
                        Symbol = input.Symbol,
                        PriceInUsd = price.PriceInUsd
                    });
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
        
        private int GetTokenPriority(string token)
        {
            switch (token)
            {
                //todo from config
                // case "ELF": return 1;
                // case "USDT": return 2;
                // case "BNB": return 3;
                default: return 0; 
            }
        }
        
        private bool CanPriceFrom(string from, string to, TradePairsGraph graph)
        {
            return graph.Relations.ContainsKey(from) && graph.Relations[from].ContainsKey(to);
        }

        private string GetHighestPriorityTradePairAddress(string from, string to, TradePairsGraph graph)
        {
            if (!graph.Relations.ContainsKey(from) || !graph.Relations[from].ContainsKey(to))
            {
                return null;
            }

            var sortedTradePairs = graph.Relations[from][to]
                .OrderByDescending(tradePair => tradePair.ValueLocked0 + tradePair.ValueLocked1);

            return sortedTradePairs.FirstOrDefault()?.Address;
        }

        private async Task<Tuple<List<string>,List<string>>> GetPriceTokensAsync(TradePairsGraph graph)
        {
            var withPriceTokens = new List<string>();
            var noPriceTokens = new List<string>();
            foreach (var relation in graph.Relations)
            {
                var key = $"{PriceOptions.PriceCachePrefix}:{relation.Key}";
                var price = await _priceCache.GetAsync(key);
                if (price != null)
                {
                    withPriceTokens.Add(relation.Key);
                }
                else
                {
                    noPriceTokens.Add(relation.Key);
                }
            }

            return new Tuple<List<string>, List<string>>(withPriceTokens,noPriceTokens);
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
            var tokensWithUSDPrice = priceTokens.Item1;
            var tokensNoPrice = priceTokens.Item2;
            var priceSpreadTrees = new List<PricingNode>();
            foreach (var token in tokensWithUSDPrice)
            {
                priceSpreadTrees.Add(new PricingNode
                {
                    FromTokenSymbol = null,
                    TokenSymbol = token,
                    Depth = 0,
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

                foreach (var node in currentLayer)
                {
                    List<string> tokensToPrice = tokensNoPrice.Where(to => CanPriceFrom(node.TokenSymbol, to, graph)).ToList();

                    foreach (var tokenToPrice in tokensToPrice)
                    {
                        string tradePairAddress = GetHighestPriorityTradePairAddress(node.TokenSymbol, tokenToPrice, graph);

                        node.ToTokens.Add(new PricingNode
                        {
                            FromTokenSymbol = node.TokenSymbol,
                            TokenSymbol = tokenToPrice,
                            Depth = currentDepth,
                            FromTradePairAddress = tradePairAddress,
                            ToTokens = new List<PricingNode>()
                        });

                        tokensNoPrice.Remove(tokenToPrice);
                        tokensWithUSDPrice.Add(tokenToPrice);
                    }
                }

                currentDepth++;
            }
            return new TokenPricingMap()
            {
                PriceSpreadTrees = priceSpreadTrees
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

        public async Task UpdatePricingMapAsync(string chainId)
        {
            var tradePairList = await GetTradePairAsync(chainId);
            var tokenPricingMap = await BuildPriceSpreadTrees(tradePairList);
            // todo
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
    }
    
    public class PricingNode
    {
        public int Depth { get; set; }
        public string FromTokenSymbol { get; set; }
        public string FromTradePairAddress { get; set; }
        public string TokenSymbol { get; set; }
        public List<PricingNode> ToTokens { get; set; }
    }

    public class TradePairsGraph
    {
        public Dictionary<string, Dictionary<string, List<TradePair>>> Relations { get; set; } = new();
    }
}