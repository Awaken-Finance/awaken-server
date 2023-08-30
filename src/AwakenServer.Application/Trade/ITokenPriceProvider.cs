using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Chains;
using AwakenServer.Price;
using AwakenServer.Tokens;
using AwakenServer.Trade.Dtos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Trade
{
    public interface ITokenPriceProvider
    {
        Task<double> GetTokenUSDPriceAsync(string chainId, string symbol);
        Task UpdatePriceAsync(string chainId, Guid token0, Guid token1, double price);
    }

    public class TokenPriceProvider : ITokenPriceProvider, ITransientDependency
    {
        private readonly INESTRepository<TradePairInfoIndex, Guid> _tradePairInfoIndex;
        private readonly INESTRepository<Chain, string> _chainIndexRepository;
        private readonly StableCoinOptions _stableCoinOptions;
        private readonly ITokenAppService _tokenAppService;
        private readonly ITradePairMarketDataProvider _tradePairMarketDataProvider;
        private readonly IPriceAppService _priceAppService;
        private readonly IObjectMapper _objectMapper;
        private readonly ILogger<TokenPriceProvider> _logger;
        private readonly IDistributedCache<Dictionary<Guid, TokenPrice>> _tokenPriceCache;

        private const int MaxLevel = 2;

        public TokenPriceProvider(INESTRepository<TradePairInfoIndex, Guid> tradePairInfoIndex,
            IDistributedCache<Dictionary<Guid, TokenPrice>> tokenPriceCache,
            IOptionsSnapshot<StableCoinOptions> stableCoinOptions, INESTRepository<Chain, string> chainIndexRepository,
            TokenAppService tokenAppService, ITradePairMarketDataProvider tradePairMarketDataProvider,
            IPriceAppService priceAppService,
            IObjectMapper objectMapper,
            ILogger<TokenPriceProvider> logger)
        {
            _tradePairInfoIndex = tradePairInfoIndex;
            _tokenPriceCache = tokenPriceCache;
            _chainIndexRepository = chainIndexRepository;
            _tokenAppService = tokenAppService;
            _tradePairMarketDataProvider = tradePairMarketDataProvider;
            _stableCoinOptions = stableCoinOptions.Value;
            _priceAppService = priceAppService;
            _objectMapper = objectMapper;
            _logger = logger;
        }

        public async Task<double> GetTokenUSDPriceAsync(string chainId, string symbol)
        {
            var price = await _priceAppService.GetTokenPriceListAsync(new List<string> { symbol });
            if (price == null || price.Items.Count == 0)
            {
                _logger.LogError("GetTokenUSDPriceAsync，can not find price,token:{symbol},chain:{chainId}",
                    symbol, chainId);
                return 0;
            }
            
            return (double)price.Items[0].PriceInUsd;
        }

        public async Task UpdatePriceAsync(string chainId, Guid token0, Guid token1, double price)
        {
            var tokenPrices = await GetTokenPriceCacheAsync(chainId);

            await UpdatePriceAsync(chainId, tokenPrices, token1, token0, 1 / price);
            await UpdatePriceAsync(chainId, tokenPrices, token0, token1, price);

            await UpdateCacheAsync(chainId, tokenPrices);
        }

        private async Task UpdatePriceAsync(string chainId, Dictionary<Guid,TokenPrice> tokenPrices, Guid token0, Guid token1, double price)
        {
            if (tokenPrices.TryGetValue(token0, out var tokenPrice))
            {
                if (tokenPrice.PriceToken == token1)
                {
                    tokenPrice.Price = price;
                }
            }
            else
            {
                var tokenInfo0 = await _tokenAppService.GetAsync(token0);
                var chain = await _chainIndexRepository.GetAsync(chainId);
                if (_stableCoinOptions.Coins[chain.Name]
                    .FirstOrDefault(c => c.Address == tokenInfo0.Address && c.Symbol == tokenInfo0.Symbol) != null)
                {
                    tokenPrices[token0] = new TokenPrice
                    {
                        Level = 0,
                        PriceToken = Guid.Empty,
                        Price = 1
                    };
                }
                else if (tokenPrices.TryGetValue(token1, out var token1Price) && token1Price.Level < MaxLevel)
                {
                    tokenPrices[token0] = new TokenPrice
                    {
                        Level = token1Price.Level + 1,
                        PriceToken = token1,
                        Price = price
                    };
                }
            }
        }

        private async Task<Dictionary<Guid, TokenPrice>> GetTokenPriceCacheAsync(string chainId)
        {
            var tokenPrices = await _tokenPriceCache.GetAsync(chainId);
            if (tokenPrices != null)
            {
                return tokenPrices;
            }

            var mustQuery = new List<Func<QueryContainerDescriptor<TradePairInfoIndex>, QueryContainer>>();
            mustQuery.Add(q => q.Term(t => t.Field(f => f.ChainId).Value(chainId)));
            QueryContainer Filter(QueryContainerDescriptor<TradePairInfoIndex> f) => f.Bool(b => b.Must(mustQuery));
            var tradePairInfo = await _tradePairInfoIndex.GetListAsync(Filter);
            var pools = _objectMapper.Map<List<TradePairInfoIndex>,List<TradePair>>(tradePairInfo.Item2);
            
            var chain = await _chainIndexRepository.GetAsync(chainId);
            var stableCoins = _stableCoinOptions.Coins[chain.Name];
            tokenPrices = new Dictionary<Guid, TokenPrice>();
            var rootTokens = new HashSet<Guid>();
            foreach (var coin in stableCoins)
            {
                var token = await _tokenAppService.GetAsync(new GetTokenInput
                {
                    ChainId = chainId,
                    Address = coin.Address,
                    Symbol = coin.Symbol
                });

                if (token == null)
                {
                    continue;
                }

                tokenPrices[token.Id] = new TokenPrice
                {
                    Level = 0,
                    Price = 1,
                    PriceToken = Guid.Empty
                };
                rootTokens.Add(token.Id);
            }

            await InitPriceAsync(tokenPrices, pools, 1, rootTokens);
            await UpdateCacheAsync(chainId, tokenPrices);
            return tokenPrices;
        }

        private async Task UpdateCacheAsync(string chainId, Dictionary<Guid, TokenPrice> cache)
        {
            await _tokenPriceCache.SetAsync(chainId, cache, new DistributedCacheEntryOptions()
            {
                AbsoluteExpiration = DateTimeOffset.MaxValue
            });
        }

        private async Task InitPriceAsync(Dictionary<Guid, TokenPrice> tokenPrices, List<TradePair> allPools, int level,
            HashSet<Guid> rootTokens)
        {
            while (true)
            {
                if (level > MaxLevel)
                {
                    return;
                }

                var newRootTokens = new HashSet<Guid>();
                foreach (var rootToken in rootTokens)
                {
                    var pools = allPools.Where(p => p.Token0Id == rootToken || p.Token1Id == rootToken).ToList();

                    foreach (var pool in pools)
                    {
                        var marketData = await _tradePairMarketDataProvider.GetLatestTradePairMarketDataAsync(pool.ChainId,pool.Id);
                        var price = marketData?.Price ?? 0;
                        if (pool.Token0Id == rootToken)
                        {
                            if (tokenPrices.ContainsKey(pool.Token1Id))
                            {
                                continue;
                            }

                            tokenPrices[pool.Token1Id] = new TokenPrice
                            {
                                Level = level, PriceToken = pool.Token0Id, Price = price == 0 ? 0 : 1 / price
                            };
                            newRootTokens.Add(pool.Token1Id);
                        }
                        else
                        {
                            if (tokenPrices.ContainsKey(pool.Token0Id))
                            {
                                continue;
                            }

                            tokenPrices[pool.Token0Id] = new TokenPrice
                                {Level = level, PriceToken = pool.Token1Id, Price = price};
                            newRootTokens.Add(pool.Token0Id);
                        }
                    }
                }

                level += 1;
                rootTokens = newRootTokens;
            }
        }
    }

    public class TokenPrice
    {
        public double Price { get; set; }
        public Guid PriceToken { get; set; }
        public int Level { get; set; }
    }
}