using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Chains;
using AwakenServer.CMS;
using AwakenServer.Favorite;
using AwakenServer.Provider;
using AwakenServer.Tokens;
using AwakenServer.Trade.Dtos;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using Nest;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.EventBus.Distributed;
using Token = AwakenServer.Tokens.Token;

namespace AwakenServer.Trade
{
    [RemoteService(IsEnabled = false)]
    public class TradePairAppService : ApplicationService, ITradePairAppService
    {
        private readonly INESTRepository<TradePairInfoIndex, Guid> _tradePairInfoIndex;
        private readonly ITokenPriceProvider _tokenPriceProvider;
        private readonly ITokenAppService _tokenAppService;
        private readonly IBlockchainAppService _blockchainAppService;
        private readonly INESTRepository<Index.TradePair, Guid> _tradePairIndexRepository;
        private readonly ITradePairMarketDataProvider _tradePairMarketDataProvider;
        private readonly ITradeRecordAppService _tradeRecordAppService;
        private readonly IFavoriteAppService  _favoriteAppService;
        private readonly ICmsAppService _cmsAppService;
        private readonly IChainAppService _chainAppService;
        private readonly IGraphQLProvider _graphQlProvider;
        private readonly ILogger<TradePairAppService> _logger;
        private readonly IDistributedEventBus _distributedEventBus;
        private readonly IBus _bus;
        
        private const string ASC = "asc";
        private const string ASCEND = "ascend";
        private const string PRICE = "price";
        private const string PRICEUSD = "priceusd";
        private const string VOLUMEPERCENTCHANGE24H = "volumepercentchange24h";
        private const string PRICEHIGH24H = "pricehigh24h";
        private const string PRICEHIGH24HUSD = "pricehigh24husd";
        private const string PRICELOW24H = "pricelow24h";
        private const string PRICELOW24HUSD = "pricelow24husd";
        private const string FEEPERCENT7D = "feepercent7d";
        private const string TVL = "tvl";
        private const string PRICEPERCENTCHANGE24H = "pricepercentchange24h";
        private const string VOLUME24H = "volume24h";
        private const string TRADEPAIR = "tradepair";

        public TradePairAppService(INESTRepository<TradePairInfoIndex, Guid> tradePairInfoIndex,
            ITokenPriceProvider tokenPriceProvider,
            IGraphQLProvider iGraphQlProvider,
            INESTRepository<Index.TradePair, Guid> tradePairIndexRepository,
            ITradePairMarketDataProvider tradePairMarketDataProvider,
            ITradeRecordAppService tradeRecordAppService,
            IDistributedEventBus distributedEventBus,
            ITokenAppService tokenAppService,
            IChainAppService chainAppService,
            IFavoriteAppService favoriteAppService,
            IBlockchainAppService blockchainAppService,
            ICmsAppService cmsAppService,
            IBus bus,
            ILogger<TradePairAppService> logger)
        {
            _tradePairInfoIndex = tradePairInfoIndex;
            _tokenPriceProvider = tokenPriceProvider;
            _graphQlProvider = iGraphQlProvider;
            _tradePairIndexRepository = tradePairIndexRepository;
            _tradePairMarketDataProvider = tradePairMarketDataProvider;
            _tradeRecordAppService = tradeRecordAppService;
            _distributedEventBus = distributedEventBus;
            _tokenAppService = tokenAppService;
            _chainAppService = chainAppService;
            _favoriteAppService = favoriteAppService;
            _blockchainAppService = blockchainAppService;
            _cmsAppService = cmsAppService;
            _logger = logger;
            _bus = bus;
        }

        public async Task<List<TradePairDto>> GetTradePairInfoListAsync(GetTradePairsInfoInput input)
        {
            var tradePairInfoDtoPageResultDto = await _graphQlProvider.GetTradePairInfoListAsync(input);
            return tradePairInfoDtoPageResultDto.GetTradePairInfoList.Data.Count == 0
                ? new List<TradePairDto>()
                : ObjectMapper.Map<List<TradePairInfoDto>, List<TradePairDto>>(tradePairInfoDtoPageResultDto.GetTradePairInfoList.Data);
        }

        public async Task<PagedResultDto<TradePairIndexDto>> GetListAsync(GetTradePairsInput input)
        {
            var chainDto = await _chainAppService.GetByNameCacheAsync(input.ChainId);
            return chainDto == null
                ? new PagedResultDto<TradePairIndexDto>()
                : await GetPairListAsync(input, new List<Guid>());
        }

        public async Task<TradePairDto> GetTradePairInfoAsync(Guid id)
        {
            var result = await _graphQlProvider.GetTradePairInfoListAsync(new GetTradePairsInfoInput
            {
                Id = id.ToString()
            });

            return ObjectMapper.Map<TradePairInfoDto, TradePairDto>(result.GetTradePairInfoList.Data.FirstOrDefault());
        }

        public async Task<TradePairIndexDto> GetAsync(Guid id)
        {
            return ObjectMapper.Map<Index.TradePair, TradePairIndexDto>(
                await _tradePairIndexRepository.GetAsync(id));
        }

        public async Task<TradePairIndexDto> GetByAddressAsync(Guid id, [CanBeNull] string address)
        {
            var tradePair = await _tradePairIndexRepository.GetAsync(id);
            if (tradePair == null)
            {
                return new TradePairIndexDto();
            }
            var tradePairDto = ObjectMapper.Map<Index.TradePair, TradePairIndexDto>(tradePair);

            if (string.IsNullOrEmpty(address)) return tradePairDto;
            
            var favoriteList = await _favoriteAppService.GetListAsync(address);
            if (favoriteList != null && favoriteList.Any(favorite => favorite.TradePairId == tradePair.Id))
            {
                tradePairDto.IsFav = true;
                tradePairDto.FavId = favoriteList.First(favorite => favorite.TradePairId == tradePair.Id).Id;
            }

            return tradePairDto;
        }


        public async Task<TradePairIndexDto> GetTradePairAsync(string chainId, string address)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<Index.TradePair>, QueryContainer>>();
            if (!string.IsNullOrEmpty(chainId))
            {
                mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
            }
            
            if (!string.IsNullOrEmpty(address))
            {
                mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(address)));
            }
            
            QueryContainer Filter(QueryContainerDescriptor<Index.TradePair> f) => f.Bool(b => b.Must(mustQuery));
            
            var list = await _tradePairIndexRepository.GetListAsync(Filter, limit: 1);
            return ObjectMapper.Map<Index.TradePair, TradePairIndexDto>(list.Item2.FirstOrDefault());
        }

        public async Task<ListResultDto<TradePairIndexDto>> GetByIdsAsync(GetTradePairByIdsInput input)
        {
            if (input.Ids == null || input.Ids.Count == 0)
            {
                return new ListResultDto<TradePairIndexDto>();
            }
            
            var inputDto =  ObjectMapper.Map<GetTradePairByIdsInput, GetTradePairsInput>(input);

            return await GetPairListAsync(inputDto, input.Ids);
        }

        public async Task<TokenListDto> GetTokenListAsync(GetTokenListInput input)
        {
            var pairs = await _tradePairIndexRepository.GetListAsync(q =>
                q.Term(i => i.Field(f => f.ChainId).Value(input.ChainId)));
            var token0 = new Dictionary<Guid, Tokens.Token>();
            var token1 = new Dictionary<Guid, Tokens.Token>();

            foreach (var pair in pairs.Item2)
            {
                if (!token0.ContainsKey(pair.Token0.Id))
                {
                    token0.TryAdd(pair.Token0.Id, pair.Token0);
                }

                if (!token1.ContainsKey(pair.Token1.Id))
                {
                    token1.TryAdd(pair.Token1.Id, pair.Token1);
                }
            }

            return new TokenListDto
            {
                Token0 = ObjectMapper.Map<List<Tokens.Token>, List<TokenDto>>(token0.Values.ToList()),
                Token1 = ObjectMapper.Map<List<Tokens.Token>, List<TokenDto>>(token1.Values.ToList())
            };
        }

        public async Task<TradePairDto> GetByAddressAsync(string chainName, [CanBeNull] string address)
        {
            var result = await _graphQlProvider.GetTradePairInfoListAsync(new GetTradePairsInfoInput
            {
                ChainId = chainName,
                Address = address
            });

            return ObjectMapper.Map<TradePairInfoDto, TradePairDto>(result.GetTradePairInfoList.Data.FirstOrDefault());
        }

        public async Task<List<TradePairIndexDto>> GetListAsync(string chainId, IEnumerable<string> addresses)
        {
            QueryContainer Filter(QueryContainerDescriptor<Index.TradePair> q) =>
                q.Term(i => i.Field(f => f.ChainId).Value(chainId)) &&
                q.Terms(i => i.Field(f => f.Address).Terms(addresses));

            var list = await _tradePairIndexRepository.GetListAsync(Filter,
                limit: addresses.Count(), skip: 0);
            return ObjectMapper.Map<List<Index.TradePair>, List<TradePairIndexDto>>(list.Item2);
        }

        /// <summary>
        /// this function is for unit test and some unuse processor
        /// the only way to create trade pair is timer in TradePairSyncWorker
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<TradePairDto> CreateAsync(TradePairCreateDto input)
        {
            if (input.Id == Guid.Empty)
            {
                input.Id = Guid.NewGuid();
            }

            var token0 = await _tokenAppService.GetAsync(input.Token0Id);
            var token1 = await _tokenAppService.GetAsync(input.Token1Id);
            var tradePair = ObjectMapper.Map<TradePairCreateDto, TradePairInfoIndex>(input);
            tradePair.Token0Symbol = token0.Symbol;
            tradePair.Token1Symbol = token1.Symbol;
            await _tradePairInfoIndex.AddOrUpdateAsync(tradePair);
            var index = ObjectMapper.Map<TradePairCreateDto, Index.TradePair>(input);
            index.Token0 = ObjectMapper.Map<TokenDto, Token>(token0);
            index.Token1 = ObjectMapper.Map<TokenDto, Token>(token1);

            await _tradePairIndexRepository.AddOrUpdateAsync(index);
            
            return ObjectMapper.Map<TradePairInfoIndex, TradePairDto>(tradePair);
        }

        public async Task UpdateLiquidityAsync(LiquidityUpdateDto input)
        {
            var result = await _tradePairIndexRepository.GetAsync(input.TradePairId);
            if (result == null)
            {
                _logger.LogError("UpdateLiquidityAsync:can not find trade pair id: {tradePairId},chainId: {chainId}",
                    input.TradePairId, input.ChainId);
                return;
            }

            var tradePair = ObjectMapper.Map<Index.TradePair, TradePairDto>(result);

            var marketData =
                await _tradePairMarketDataProvider.GetLatestTradePairMarketDataAsync(input.ChainId, input.TradePairId);
            var timestamp = DateTimeHelper.FromUnixTimeMilliseconds(input.Timestamp);
            var price = double.Parse(input.Token1Amount) / double.Parse(input.Token0Amount);

            /*if (marketData == null || marketData.Timestamp < timestamp)
            {
                await _tokenPriceProvider.UpdatePriceAsync(input.ChainId, tradePair.Token0Id, tradePair.Token1Id,
                    price);
            }*/

            var priceUSD0 = await _tokenPriceProvider.GetTokenUSDPriceAsync(tradePair.ChainId, tradePair.Token0Symbol);
            var priceUSD1 = await _tokenPriceProvider.GetTokenUSDPriceAsync(tradePair.ChainId, tradePair.Token1Symbol);
            var tvl = priceUSD0 * double.Parse(input.Token0Amount) + priceUSD1 * double.Parse(input.Token1Amount);
            _logger.LogInformation(
                "pair:id:{id},{token0Symbol}-{token1Symbol},fee:{fee},price:{price}-priceUSD:{priceUSD},token1:{token1}-priceUSD1:{priceUSD1},tvl:{tvl}",
                tradePair.Id, tradePair.Token0Symbol, tradePair.Token1Symbol, tradePair.FeeRate,
                price, priceUSD1 != 0 ? price * priceUSD1 : priceUSD0, tradePair.Token1Symbol, priceUSD1, tvl);

            await _tradePairMarketDataProvider.UpdateLiquidityAsync(tradePair.ChainId, tradePair.Id, timestamp, price,
                priceUSD1 != 0 ? price * priceUSD1 : priceUSD0, tvl, double.Parse(input.Token0Amount),
                double.Parse(input.Token1Amount));
        }

        public async Task UpdateLiquidityAsync(SyncRecordDto dto)
        {
            var pair = await GetAsync(dto.ChainId, dto.PairAddress);
            var isReversed = pair.Token0.Symbol == dto.SymbolB;
            var token0Amount = isReversed
                ? dto.ReserveB.ToDecimalsString(pair.Token0.Decimals)
                : dto.ReserveA.ToDecimalsString(pair.Token0.Decimals);
            var token1Amount = isReversed
                ? dto.ReserveA.ToDecimalsString(pair.Token1.Decimals)
                : dto.ReserveB.ToDecimalsString(pair.Token1.Decimals);

            _logger.LogInformation("SyncEvent, input chainId: {chainId}, isReversed: {isReversed}, token0Amount: {token0Amount}, " +
                "token1Amount: {token1Amount}, tradePairId: {tradePairId}, timestamp: {timestamp}, blockHeight: {blockHeight}", dto.ChainId, 
                isReversed, token0Amount, token1Amount, pair.Id, dto.Timestamp, dto.BlockHeight);
            await UpdateLiquidityAsync(new LiquidityUpdateDto
            {
                ChainId = dto.ChainId,
                TradePairId = pair.Id,
                Token0Amount = token0Amount,
                Token1Amount = token1Amount,
                Timestamp = dto.Timestamp
            });
        }

        public async Task CreateTradePairIndexAsync(TradePairInfoDto input, TokenDto token0, TokenDto token1,ChainDto chain)
        {
            var tradePair = ObjectMapper.Map<TradePairInfoDto, Index.TradePair>(input);
            tradePair.Token0 = ObjectMapper.Map<TokenDto, Token>(token0);
            tradePair.Token1 = ObjectMapper.Map<TokenDto, Token>(token1);
            tradePair.ChainId = chain.Id;
            await _tradePairIndexRepository.AddOrUpdateAsync(tradePair);
        }

        public async Task UpdateTradePairAsync(Guid id)
        {
            var timestamp = _tradePairMarketDataProvider.GetSnapshotTime(DateTime.UtcNow);
            var pair = await _tradePairIndexRepository.GetAsync(id);

            if (pair == null || !await IsNeedUpdateAsync(pair, timestamp))
            {
                if (pair == null)
                {
                    _logger.LogInformation("can not find trade pair id:{id}", id);
                }
                return;
            }

            var snapshots =
                await _tradePairMarketDataProvider.GetIndexListAsync(pair.ChainId, pair.Id, timestamp.AddDays(-2));

            var volume24h = 0d;
            var tradeValue24h = 0d;
            var tradeCount24h = 0;
            var priceHigh24h = pair.Price;
            var priceLow24h = pair.Price;
            var priceHigh24hUSD = pair.PriceUSD;
            var priceLow24hUSD = pair.PriceUSD;
            var daySnapshot = snapshots.Where(s => s.Timestamp >= timestamp.AddDays(-1)).ToList();
            foreach (var snapshot in daySnapshot)
            {
                volume24h += snapshot.Volume;
                tradeValue24h += snapshot.TradeValue;
                tradeCount24h += snapshot.TradeCount;
                priceHigh24h = Math.Max(priceHigh24h, snapshot.PriceHigh);
                priceLow24h = Math.Min(priceLow24h, snapshot.PriceLow);
                priceHigh24hUSD = Math.Max(priceHigh24hUSD, snapshot.PriceHighUSD);
                priceLow24hUSD = Math.Min(priceLow24hUSD, snapshot.PriceLowUSD);
            }

            var lastDaySnapshot = snapshots.Where(s => s.Timestamp < timestamp.AddDays(-1))
                .OrderByDescending(s => s.Timestamp).ToList();
            var lastDayVolume24h = lastDaySnapshot.Sum(snapshot => snapshot.Volume);
            var lastDayTvl = 0d;
            var lastDayPriceUSD = 0d;
            if (lastDaySnapshot.Count > 0)
            {
                var snapshot = lastDaySnapshot.First();
                lastDayTvl = snapshot.TVL;
                lastDayPriceUSD = snapshot.PriceUSD;
            }
            else
            {
                var sortDaySnapshot = daySnapshot.OrderBy(s => s.Timestamp).ToList();
                if (sortDaySnapshot.Count > 0)
                {
                    var snapshot = sortDaySnapshot.First();
                    lastDayTvl = snapshot.TVL;
                    lastDayPriceUSD = snapshot.PriceUSD;
                }
            }

            var priceUSD0 = await _tokenPriceProvider.GetTokenUSDPriceAsync(pair.ChainId, pair.Token0.Symbol);
            var priceUSD1 = await _tokenPriceProvider.GetTokenUSDPriceAsync(pair.ChainId, pair.Token1.Symbol);
            pair.PriceUSD = priceUSD1 != 0 ? pair.Price * priceUSD1 : priceUSD0;
            pair.PricePercentChange24h = lastDayPriceUSD == 0
                ? 0
                : (pair.PriceUSD - lastDayPriceUSD) * 100 / lastDayPriceUSD;
            pair.PriceChange24h  = lastDayPriceUSD == 0
                ? 0
                : pair.PriceUSD - lastDayPriceUSD;
            pair.TVL = priceUSD0 * pair.ValueLocked0 + priceUSD1 * pair.ValueLocked1;
            pair.TVLPercentChange24h = lastDayTvl == 0
                ? 0
                : (pair.TVL - lastDayTvl) * 100 / lastDayTvl;
            pair.PriceHigh24h = priceHigh24h;
            pair.PriceHigh24hUSD = priceHigh24hUSD;
            pair.PriceLow24hUSD = priceLow24hUSD;
            pair.PriceLow24h = priceLow24h;
            pair.Volume24h = volume24h;
            pair.VolumePercentChange24h = lastDayVolume24h == 0
                ? 0
                : (pair.Volume24h - lastDayVolume24h) * 100 / lastDayVolume24h;
            pair.TradeValue24h = tradeValue24h;
            pair.TradeCount24h = tradeCount24h;
            pair.FeePercent7d = await GetFeePercent7dAsync(pair, timestamp);
            pair.TradeAddressCount24h = await _tradeRecordAppService.GetUserTradeAddressCountAsync(pair.ChainId, pair.Id, timestamp);

            _logger.LogInformation(
                "updatePairTimer token:{token0Symbol}-{token1Symbol},fee:{fee}-price:{price}-priceUSD:{priceUSD},token1:{token1}-priceUSD1:{priceUSD1}",
                pair.Token0.Symbol, pair.Token1.Symbol, pair.FeeRate, pair.Price, pair.PriceUSD, pair.Token1.Symbol, priceUSD1);
            
            await _tradePairIndexRepository.AddOrUpdateAsync(pair);

            await _bus.Publish<NewIndexEvent<TradePairIndexDto>>(new NewIndexEvent<TradePairIndexDto>
            {
                Data = ObjectMapper.Map<Index.TradePair, TradePairIndexDto>(pair)
            });
            /*await _distributedEventBus.PublishAsync(new NewIndexEvent<TradePairIndexDto>
            {
                Data = ObjectMapper.Map<Index.TradePair, TradePairIndexDto>(pair)
            });*/
        }

        public async Task SyncTokenAsync(TradePairInfoDto pair, ChainDto chain)
        {
            var tokenDto = await _tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = pair.ChainId,
                Symbol = pair.Token0Symbol,
            });
            
            if (tokenDto == null)
            {
                var tokenInfo =
                    await _blockchainAppService.GetTokenInfoAsync(pair.ChainId, null, pair.Token0Symbol);

                var token = await _tokenAppService.CreateAsync(new TokenCreateDto
                {
                    Address = tokenInfo.Address,
                    Decimals = tokenInfo.Decimals,
                    Symbol = tokenInfo.Symbol,
                    ChainId = chain.Id
                });
                _logger.LogInformation("token created: Id:{id},ChainId:{chainId},Symbol:{symbol},Decimal:{decimal}", token.Id,
                    token.ChainId, token.Symbol, token.Decimals);
            }


            tokenDto = await _tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = pair.ChainId,
                Symbol = pair.Token1Symbol,
            });

            if (tokenDto == null)
            {
                var tokenInfo =
                    await _blockchainAppService.GetTokenInfoAsync(pair.ChainId, null, pair.Token1Symbol);

                var token = await _tokenAppService.CreateAsync(new TokenCreateDto
                {
                    Address = tokenInfo.Address,
                    Decimals = tokenInfo.Decimals,
                    Symbol = tokenInfo.Symbol,
                    ChainId = chain.Id
                });
                _logger.LogInformation("token created: Id:{id},ChainId:{chainId},Symbol:{symbol},Decimal:{decimal}", token.Id,
                    token.ChainId, token.Symbol, token.Decimals);
            }
        }

        public async Task SyncPairAsync(TradePairInfoDto pair, ChainDto chain)
        {
            if (!Guid.TryParse(pair.Id, out var pairId))
            {
                _logger.LogError("pairId is not valid:{pairId},chainName:{chainName},token0:{token0Symbol},token1:{token1Symbol}", 
                    pair.Id, chain.Name, pair.Token0Symbol, pair.Token1Symbol);
                return;
            }

            var existPair = await GetTradePairAsync(pair.ChainId, pair.Address);
            if (existPair != null)
            {
                return;
            }

            var token0 = await _tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = chain.Id,
                Symbol = pair.Token0Symbol,
            });
            var token1 = await _tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = chain.Id,
                Symbol = pair.Token1Symbol,
            });
            
            if (token0 == null)
            {
                _logger.LogInformation("can not find token {token0Symbol},chainId:{chainId},pairId:{pairId}", 
                    pair.Token0Symbol, chain.Id, pair.Id);
            }

            if (token1 == null)
            {
                _logger.LogInformation("can not find token {token1Symbol},chainId:{chainId},pairId:{pairId}", 
                    pair.Token1Symbol, chain.Id, pair.Id);
            }

            if (token0 == null || token1 == null) return;

            _logger.LogInformation("create pair success Id:{pairId},chainId:{chainId},token0:{token0}," +
                                   "token1:{token1}",pair.Id, chain.Id, pair.Token0Symbol, pair.Token1Symbol);
            await CreateTradePairIndexAsync(pair, token0, token1,chain);
        }

        public async Task DeleteManyAsync(List<Guid> ids)
        {
            foreach (var id in ids)
            {
                await _tradePairInfoIndex.DeleteAsync(id);
            }
        }
        
        private async Task<PagedResultDto<TradePairIndexDto>> GetPairListAsync(GetTradePairsInput input,
            List<Guid> idList)
        {
            var queryBuilder = await new TradePairListQueryBuilder(_cmsAppService, _favoriteAppService)
                .WithChainId(input.ChainId)
                .WithIdList(idList)
                .WithToken0Id(input.Token0Id)
                .WithToken1Id(input.Token1Id)
                .WithToken0Symbol(input.Token0Symbol)
                .WithToken1Symbol(input.Token1Symbol)
                .WithFeeRate(input.FeeRate)
                .WithIdList(idList)
                .WithTokenSymbol(input.TokenSymbol)
                .WithSearchTokenSymbol(input.SearchTokenSymbol)
                .WithTradePairFeatureAsync(input.ChainId, input.Address, input.TradePairFeature);

            var mustQuery = queryBuilder.Build();
            QueryContainer Filter(QueryContainerDescriptor<Index.TradePair> f) => f.Bool(b => b.Must(mustQuery));

            var sorting = GetSort(input.Sorting, input.Page);
            var list = await _tradePairIndexRepository.GetSortListAsync(Filter,
                sortFunc: sorting,
                limit: input.MaxResultCount == 0 ? TradePairConst.MaxPageSize :
                input.MaxResultCount > TradePairConst.MaxPageSize ? TradePairConst.MaxPageSize : input.MaxResultCount,
                skip: input.SkipCount);

            var totalCount = await _tradePairIndexRepository.CountAsync(Filter);
            
            var items = ObjectMapper.Map<List<Index.TradePair>, List<TradePairIndexDto>>(list.Item2);

            try
            {
                items = await AddFavoriteInfoAsync(items, input);
            }catch(Exception e)
            {
                _logger.LogError(e, "add favorite info error");
            }
       
            return new PagedResultDto<TradePairIndexDto>
            {
                Items = items,
                TotalCount = totalCount.Count
            };
        }

        private async Task<List<TradePairIndexDto>> AddFavoriteInfoAsync(List<TradePairIndexDto> inTradePairIndexDtos,
            GetTradePairsInput input)
        {
            if (string.IsNullOrEmpty(input.Address) || inTradePairIndexDtos.Count == 0)
            {
                return inTradePairIndexDtos;
            }
            
            var favoriteList = await _favoriteAppService.GetListAsync(input.Address);

            if (favoriteList.Count == 0)
            {
                return inTradePairIndexDtos;
            }

            var favoriteDictionary = favoriteList?.ToDictionary(favorite => favorite.TradePairId);
            foreach (var tradePair in inTradePairIndexDtos)
            {
                if (favoriteDictionary.TryGetValue(tradePair.Id, out var favorite))
                {
                    tradePair.IsFav = true;
                    tradePair.FavId = favorite.Id;
                }
            }

            return inTradePairIndexDtos;
        }

        private async Task<Index.TradePair> GetAsync(string chainName, string address)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<Index.TradePair>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainName)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(address)));

            QueryContainer Filter(QueryContainerDescriptor<Index.TradePair> f) => f.Bool(b => b.Must(mustQuery));
            return await _tradePairIndexRepository.GetAsync(Filter);
        }

        private async Task<bool> IsNeedUpdateAsync(Index.TradePair pair, DateTime time)
        {
            var lastSnapshot =
                await _tradePairMarketDataProvider.GetLatestTradePairMarketDataIndexAsync(pair.ChainId, pair.Id);
            return lastSnapshot != null && lastSnapshot.Timestamp < time.AddHours(-1);
        }

        private async Task<double> GetFeePercent7dAsync(Index.TradePair pair, DateTime time)
        {
            if (pair.TVL == 0)
            {
                return 0;
            }

            var volume7d =
                (await _tradePairMarketDataProvider.GetIndexListAsync(pair.ChainId, pair.Id, time.AddDays(-7))).Sum(k =>
                    k.Volume);
            return (volume7d * pair.PriceUSD * pair.FeeRate * 365 * 100) / (pair.TVL * 7);
        }

        private static Func<SortDescriptor<Index.TradePair>, IPromise<IList<ISort>>> GetDefaultSort(TradePairPage page)
        {
            switch (page)
            {
                case TradePairPage.MarketPage:
                    return descriptor => descriptor.Descending(f => f.Volume24h).Descending(f => f.TVL).Descending(f => f.Price)
                        .Descending(f => f.FeeRate);;
                case TradePairPage.TradePage:
                    return descriptor => descriptor.Ascending(f => f.FeeRate);
                default:
                    return descriptor => descriptor.Ascending(f => f.Token0.Symbol);;
            }
        }

        private static Func<SortDescriptor<Index.TradePair>, IPromise<IList<ISort>>> GetSort(string sorting,
            TradePairPage page)
        {
            if (string.IsNullOrWhiteSpace(sorting))
            {
                return GetDefaultSort(page);
            }

            var sortingArray = sorting.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            Func<SortDescriptor<Index.TradePair>, IPromise<IList<ISort>>> sortDescriptor;
            switch (sortingArray.Length)
            {
                case 1:
                    sortDescriptor = GetSortDescriptorForSingleColumn(sortingArray[0]);
                    break;
                case 2:
                    var sortOrder = sortingArray[1].Trim();
                    var order = sortOrder.Equals(ASC, StringComparison.OrdinalIgnoreCase) ||
                                sortOrder.Equals(ASCEND, StringComparison.OrdinalIgnoreCase)
                        ? SortOrder.Ascending
                        : SortOrder.Descending;
                    sortDescriptor = GetSortDescriptorForDoubleColumns(sortingArray[0].Trim(), order);
                    break;
                default:
                    sortDescriptor = descriptor => descriptor.Ascending(f => f.Token0.Symbol);
                    break;
            }

            return sortDescriptor;
        }

        private static Func<SortDescriptor<Index.TradePair>, IPromise<IList<ISort>>> GetSortDescriptorForSingleColumn(
            string columnName)
        {
            switch (columnName.Trim().ToLower())
            {
                case PRICE:
                case PRICEUSD:
                    return descriptor => descriptor.Ascending(f => f.Price);
                case VOLUMEPERCENTCHANGE24H:
                    return descriptor => descriptor.Ascending(f => f.VolumePercentChange24h);
                case PRICEHIGH24H:
                case PRICEHIGH24HUSD:
                    return descriptor => descriptor.Ascending(f => f.PriceHigh24h);
                case PRICELOW24H:
                case PRICELOW24HUSD:
                    return descriptor => descriptor.Ascending(f => f.PriceLow24h);
                case FEEPERCENT7D:
                    return descriptor => descriptor.Ascending(f => f.FeePercent7d);
                case TVL:
                    return descriptor => descriptor.Ascending(f => f.TVL);
                case PRICEPERCENTCHANGE24H:
                    return descriptor => descriptor.Ascending(f => f.PricePercentChange24h);
                case VOLUME24H:
                    return descriptor => descriptor.Ascending(f => f.Volume24h);
                case TRADEPAIR:
                    return descriptor => descriptor.Ascending(f => f.Token0.Symbol).Ascending(f => f.Token1.Symbol);
                default:
                    return descriptor => descriptor.Ascending(f => f.Token0.Symbol);
            }
        }

        private static Func<SortDescriptor<Index.TradePair>, IPromise<IList<ISort>>> GetSortDescriptorForDoubleColumns(
            string columnName, SortOrder order)
        {
            switch (columnName.Trim().ToLower())
            {
                case PRICE:
                case PRICEUSD:
                    return order == SortOrder.Ascending
                        ? descriptor => descriptor.Ascending(f => f.Price)
                        : descriptor => descriptor.Descending(f => f.Price);
                case VOLUMEPERCENTCHANGE24H:
                    return order == SortOrder.Ascending
                        ? descriptor => descriptor.Ascending(f => f.VolumePercentChange24h)
                        : descriptor => descriptor.Descending(f => f.VolumePercentChange24h);
                case PRICEHIGH24H:
                case PRICEHIGH24HUSD:
                    return order == SortOrder.Ascending
                        ? descriptor => descriptor.Ascending(f => f.PriceHigh24h)
                        : descriptor => descriptor.Descending(f => f.PriceHigh24h);
                case PRICELOW24H:
                case PRICELOW24HUSD:
                    return order == SortOrder.Ascending
                        ? descriptor => descriptor.Ascending(f => f.PriceLow24h)
                        : descriptor => descriptor.Descending(f => f.PriceLow24h);
                case FEEPERCENT7D:
                    return order == SortOrder.Ascending
                        ? descriptor => descriptor.Ascending(f => f.FeePercent7d)
                        : descriptor => descriptor.Descending(f => f.FeePercent7d);
                case TVL:
                    return order == SortOrder.Ascending
                        ? descriptor => descriptor.Ascending(f => f.TVL)
                        : descriptor => descriptor.Descending(f => f.TVL);
                case PRICEPERCENTCHANGE24H:
                    return order == SortOrder.Ascending
                        ? descriptor => descriptor.Ascending(f => f.PricePercentChange24h)
                        : descriptor => descriptor.Descending(f => f.PricePercentChange24h);
                case VOLUME24H:
                    return order == SortOrder.Ascending
                        ? descriptor => descriptor.Ascending(f => f.Volume24h)
                        : descriptor => descriptor.Descending(f => f.Volume24h);
                case TRADEPAIR:
                    return order == SortOrder.Ascending
                        ? descriptor => descriptor.Ascending(f => f.Token0.Symbol).Ascending(f => f.Token1.Symbol)
                        : descriptor => descriptor.Descending(f => f.Token0.Symbol).Descending(f => f.Token1.Symbol);
                default:
                    return descriptor => descriptor.Ascending(f => f.Token0.Symbol);
            }
        }
    }
}