using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Client.MultiToken;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Chains;
using AwakenServer.CMS;
using AwakenServer.Common;
using AwakenServer.Favorite;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.SwapTokenPath;
using AwakenServer.Grains.Grain.Price;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.Route;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Provider;
using AwakenServer.Tokens;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Etos;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Orleans;
using Serilog;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;
using Token = AwakenServer.Tokens.Token;
using IObjectMapper = Volo.Abp.ObjectMapping.IObjectMapper;
using JsonConvert = Newtonsoft.Json.JsonConvert;

using TokenInfo = AElf.Contracts.MultiToken.TokenInfo;
using IndexTradePair = AwakenServer.Trade.Index.TradePair;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

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
        private readonly IFavoriteAppService _favoriteAppService;
        private readonly ICmsAppService _cmsAppService;
        private readonly IChainAppService _chainAppService;
        private readonly IGraphQLProvider _graphQlProvider;
        private readonly ILogger<TradePairAppService> _logger;
        private readonly IDistributedEventBus _distributedEventBus;
        private readonly IBus _bus;
        private readonly IClusterClient _clusterClient;
        private readonly IObjectMapper _objectMapper;
        private readonly IRevertProvider _revertProvider;
        private readonly IAElfClientProvider _blockchainClientProvider;
        private readonly ContractsTokenOptions _contractsTokenOptions;

        
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
            ILogger<TradePairAppService> logger,
            IClusterClient clusterClient,
            IObjectMapper objectMapper,
            IRevertProvider revertProvider,
            IAElfClientProvider blockchainClientProvider,
            IOptions<ContractsTokenOptions> contractsTokenOptions)
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
            _clusterClient = clusterClient;
            _objectMapper = objectMapper;
            _revertProvider = revertProvider;
            _blockchainClientProvider = blockchainClientProvider;
            _contractsTokenOptions = contractsTokenOptions.Value;

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

            return ObjectMapper.Map<TradePairInfoDto, TradePairDto>(result.TradePairInfoDtoList.Data.FirstOrDefault());
        }

        public async Task<TradePairIndexDto> GetAsync(Guid id)
        {
            return ObjectMapper.Map<Index.TradePair, TradePairIndexDto>(
                await _tradePairIndexRepository.GetAsync(id));
        }
        
        public async Task<TradePairGrainDto> GetFromGrainAsync(Guid id)
        {
            var grain = _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(id));
            
            var tradePairResult = await grain.GetAsync();
            if (!tradePairResult.Success)
            {
                return null;
            }
            
            return tradePairResult.Data;
        }

        public async Task<TradePairIndexDto> GetByAddressAsync(Guid id, [CanBeNull] string address)
        {
            var grain = _clusterClient.GetGrain<ITradePairGrain>(
                GrainIdHelper.GenerateGrainId(id));
            
            var tradePairResult = await grain.GetAsync();
            if (!tradePairResult.Success)
            {
                return new TradePairIndexDto();
            }

            var tradePair = tradePairResult.Data;
            var tradePairDto = ObjectMapper.Map<TradePairGrainDto, TradePairIndexDto>(tradePair);

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
            mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));
            QueryContainer Filter(QueryContainerDescriptor<Index.TradePair> f) => f.Bool(b => b.Must(mustQuery));

            var list = await _tradePairIndexRepository.GetListAsync(Filter);

            return ObjectMapper.Map<Index.TradePair, TradePairIndexDto>(list.Item2.FirstOrDefault());
        }

        public async Task<ListResultDto<TradePairIndexDto>> GetByIdsAsync(GetTradePairByIdsInput input)
        {
            if (input.Ids == null || input.Ids.Count == 0)
            {
                return new ListResultDto<TradePairIndexDto>();
            }

            
            var inputDto = ObjectMapper.Map<GetTradePairByIdsInput, GetTradePairsInput>(input);

            return await GetPairListAsync(inputDto, input.Ids);
        }
        
        public async Task<ListResultDto<TradePairIndexDto>> GetByIdsFromGrainAsync(GetTradePairByIdsInput input)
        {
            var items = new List<TradePairIndexDto>();
            foreach (var id in input.Ids)
            {
                var grain = _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(id));
            
                var tradePairResult = await grain.GetAsync();
                if (!tradePairResult.Success)
                {
                    Log.Error($"grain id {id} does not exist");
                    continue;
                }
            
                items.Add(_objectMapper.Map<TradePairGrainDto, TradePairIndexDto>(tradePairResult.Data));
            }
            
            return new PagedResultDto<TradePairIndexDto>
            {
                Items = items,
                TotalCount = items.Count
            };
        }

        public async Task<TokenListDto> GetTokenListAsync(GetTokenListInput input)
        {
            var grain = _clusterClient.GetGrain<IChainTradePairsGrain>(input.ChainId);
            var result = await grain.GetAsync();
            if (!result.Success)
            {
                Log.Error($"get chain trade pairs failed. chain id: {input.ChainId}");
            }

            var pairs = result.Data;
            
            // var pairs = await _tradePairIndexRepository.GetListAsync(q =>
            //     q.Term(i => i.Field(f => f.ChainId).Value(input.ChainId)));
            
            var token0 = new Dictionary<Guid, Tokens.Token>();
            var token1 = new Dictionary<Guid, Tokens.Token>();

            foreach (var pair in pairs)
            {
                if (!token0.ContainsKey(pair.Token0.Id))
                {
                    token0.TryAdd(pair.Token0.Id, _objectMapper.Map<TokenDto, Token>(pair.Token0));
                }

                if (!token1.ContainsKey(pair.Token1.Id))
                {
                    token1.TryAdd(pair.Token1.Id, _objectMapper.Map<TokenDto, Token>(pair.Token1));
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

            return ObjectMapper.Map<TradePairInfoDto, TradePairDto>(result.TradePairInfoDtoList.Data.FirstOrDefault());
        }

        public async Task<List<TradePairIndexDto>> GetListAsync(string chainId, IEnumerable<string> addresses)
        {
            var grain = _clusterClient.GetGrain<IChainTradePairsGrain>(chainId);
            var result = await grain.GetAsync(addresses);
            if (!result.Success)
            {
                Log.Error($"get chain trade pairs failed. chain id: {chainId}");
            }
            
            // QueryContainer Filter(QueryContainerDescriptor<Index.TradePair> q) =>
            //     q.Term(i => i.Field(f => f.ChainId).Value(chainId)) &&
            //     q.Terms(i => i.Field(f => f.Address).Terms(addresses));
            //
            // var list = await _tradePairIndexRepository.GetListAsync(Filter,
            //     limit: addresses.Count(), skip: 0);
            
            return ObjectMapper.Map<List<TradePairGrainDto>, List<TradePairIndexDto>>(result.Data);
        }
        
        public async Task<List<TradePairIndexDto>> GetListFromEsAsync(string chainId, IEnumerable<string> addresses)
        {
            QueryContainer Filter(QueryContainerDescriptor<Index.TradePair> q) =>
                q.Term(i => i.Field(f => f.ChainId).Value(chainId)) &&
                q.Terms(i => i.Field(f => f.Address).Terms(addresses));
            
            var list = await _tradePairIndexRepository.GetListAsync(Filter,
                limit: addresses.Count(), skip: 0);
            
            return ObjectMapper.Map<List<Index.TradePair>, List<TradePairIndexDto>>(list.Item2);
        }

        public async Task DoRevertAsync(string chainId, List<string> needDeletedTradeRecords)
        {
            if (needDeletedTradeRecords.IsNullOrEmpty())
            {
                return;
            }

            var needDeleteIndexes = await GetListAsync(chainId, needDeletedTradeRecords, 10000);
            foreach (var tradePair in needDeleteIndexes)
            {
                tradePair.IsDeleted = true;
                var grain = _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(tradePair.Id));
                await grain.AddOrUpdateAsync(_objectMapper.Map<Index.TradePair, TradePairGrainDto>(tradePair));
            }

            if (needDeleteIndexes.Count > 0)
            {
                await _tradePairIndexRepository.BulkAddOrUpdateAsync(needDeleteIndexes);
            }
        }
        
        [ExceptionHandler(typeof(Exception), TargetType = typeof(HandlerExceptionService), MethodName = nameof(HandlerExceptionService.HandleWithReturn))]
        public virtual async Task RevertTradePairAsync(string chainId)
        {
            var needDeletedTradeRecords =
                    await _revertProvider.GetNeedDeleteTransactionsAsync(EventType.TradePairEvent, chainId);
            await DoRevertAsync(chainId, needDeletedTradeRecords);
        }
        
        [ExceptionHandler(typeof(Exception), TargetType = typeof(HandlerExceptionService), MethodName = nameof(HandlerExceptionService.HandleWithReturn))]
        public virtual async Task CreateSyncAsync(SyncRecordDto dto)
        {
            var grain = _clusterClient.GetGrain<ISyncRecordGrain>(GrainIdHelper.GenerateGrainId(dto.ChainId, dto.TransactionHash, dto.PairAddress));
            if (await grain.ExistAsync())
            {
                return;
            }
            
            var pair = await GetAsync(dto.ChainId, dto.PairAddress);
            if (pair == null)
            {
                Log.Error($"get pair: {dto.PairAddress} failed in chain: {dto.ChainId}");
                return;
            }

            var isReversed = pair.Token0.Symbol == dto.SymbolB;
            var token0Amount = isReversed
                ? dto.ReserveB.ToDecimalsString(pair.Token0.Decimals)
                : dto.ReserveA.ToDecimalsString(pair.Token0.Decimals);
            var token1Amount = isReversed
                ? dto.ReserveA.ToDecimalsString(pair.Token1.Decimals)
                : dto.ReserveB.ToDecimalsString(pair.Token1.Decimals);
            
            dto.PairId = pair.Id;
            var syncRecordGrainDto = _objectMapper.Map<SyncRecordDto, SyncRecordGrainDto>(dto);
            (syncRecordGrainDto.Token0PriceInUsd, syncRecordGrainDto.Token1PriceInUsd) = await _tokenPriceProvider.GetUSDPriceAsync(pair.ChainId, pair.Id, pair.Token0.Symbol, pair.Token1.Symbol, token0Amount, token1Amount);
            
            Log.Information($"Sync event, get token price usd, trade pair id: {pair.Id}, token0: {pair.Token0.Symbol}, token1:{pair.Token1.Symbol}, price0:{syncRecordGrainDto.Token0PriceInUsd}, price1:{syncRecordGrainDto.Token1PriceInUsd}");
            
            await _tradePairMarketDataProvider.AddOrUpdateSnapshotAsync(pair.Id, async grain =>
            {
                return await grain.UpdatePriceAsync(syncRecordGrainDto);
            });
            
            await grain.AddAsync(_objectMapper.Map<SyncRecordDto, SyncRecordsGrainDto>(dto));
        }
        
        
        public async Task CreateTradePairIndexAsync(TradePairInfoDto input, TokenDto token0, TokenDto token1,
            ChainDto chain)
        {
            var tradePair = ObjectMapper.Map<TradePairInfoDto, IndexTradePair>(input);
            tradePair.Token0 = ObjectMapper.Map<TokenDto, Token>(token0);
            tradePair.Token1 = ObjectMapper.Map<TokenDto, Token>(token1);
            tradePair.ChainId = chain.Id;
            
            await _distributedEventBus.PublishAsync(new EntityCreatedEto<TradePairEto>(
                ObjectMapper.Map<IndexTradePair, TradePairEto>(tradePair)
            ));
        }

        [ExceptionHandler(typeof(Exception), Message = "GetTokenInfo Error", 
            LogLevel = LogLevel.Error, TargetType = typeof(HandlerExceptionService), MethodName = nameof(HandlerExceptionService.HandleWithReturnNull))]
        protected virtual async Task<TokenInfo> GetTokenInfoAsync(Guid tradePairId, string chainId)
        {
            var tradePairIndexDto = await GetAsync(tradePairId);

            if (tradePairIndexDto == null || !_contractsTokenOptions.Contracts.TryGetValue(
                    tradePairIndexDto.FeeRate.ToString(),
                    out var address))
            {
                Log.Error("GetTokenInfoAsync, Get tradePairIndexDto failed");
                return null;
            }

            var token = await _blockchainClientProvider.GetTokenInfoFromChainAsync(chainId, address,
                TradePairHelper.GetLpToken(tradePairIndexDto.Token0.Symbol, tradePairIndexDto.Token1.Symbol));
            Log.Information(
                $"lp token {TradePairHelper.GetLpToken(tradePairIndexDto.Token0.Symbol, tradePairIndexDto.Token1.Symbol)}, supply {token.Supply}");
            return token;
        }
        
        
        public async Task UpdateTradePairAsync(Guid id)
        {
            var snapshotTime = _tradePairMarketDataProvider.GetSnapshotTime(DateTime.UtcNow);
            
            Log.Information($"UpdateTradePairAsync id: {id}");
            
            var grain = _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(id));
            
            var pairResult = await grain.GetAsync();
            if (!pairResult.Success)
            {
                Log.Information("can not find trade pair id:{id}", id);
                return;
            }

            var pair = pairResult.Data;
            if (!await IsNeedUpdateAsync(pair, snapshotTime))
            {
                Log.Information("no need to update trade pair id:{id}", id);
                return;
            }
            
            var userTradeAddressCount = await _tradeRecordAppService.GetUserTradeAddressCountAsync(pair.ChainId, pair.Id, snapshotTime);
            var token = await GetTokenInfoAsync(pair.Id, pair.ChainId);
            var supply = token != null ? token.Supply.ToDecimalsString(token.Decimals) : "0";
            Log.Information($"get pair {pair.Id}, supply {supply}");
            
            var (token0PriceInUsd, token1PriceInUsd) = await _tokenPriceProvider.GetUSDPriceAsync(pair.ChainId, pair.Id, pair.Token0.Symbol, pair.Token1.Symbol);
            
            var tradePairGrainDtoResult = await grain.UpdateAsync(snapshotTime, userTradeAddressCount, supply, token0PriceInUsd, token1PriceInUsd);
            
            if (!tradePairGrainDtoResult.Success)
            {
                Log.Error($"AddOrUpdateTradePairIndexAsync: updage grain {pair.Id} failed");
                return;
            }
            
            await _distributedEventBus.PublishAsync(new EntityCreatedEto<TradePairEto>(
                _objectMapper.Map<TradePairGrainDto, TradePairEto>(tradePairGrainDtoResult.Data)
            ));
        }

        public async Task<TokenDto> SyncTokenAsync(string chainId, string symbol, ChainDto chain)
        {
            var tokenDto = await _tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = chainId,
                Symbol = symbol,
            });
            
            if (tokenDto == null)
            {
                Log.Information($"get token from es failed. token symbol: {symbol}, chain id: {chainId}, go to create.");
                
                var tokenInfo =
                    await _blockchainAppService.GetTokenInfoAsync(chainId, null, symbol);

                var token = await _tokenAppService.CreateAsync(new TokenCreateDto
                {
                    Address = tokenInfo.Address,
                    Decimals = tokenInfo.Decimals,
                    Symbol = tokenInfo.Symbol,
                    ImageUri = tokenInfo.ImageUri,
                    ChainId = chain.Id
                });
                
                return token;
            }

            return tokenDto;
        }

        public async Task<bool> SyncPairAsync(TradePairInfoDto pair, ChainDto chain)
        {
            if (!Guid.TryParse(pair.Id, out var pairId))
            {
                Log.Error(
                    "pairId is not valid: {pairId}, chainName: {chainName}, token0: {token0Symbol}, token1: {token1Symbol}",
                    pair.Id, chain.Name, pair.Token0Symbol, pair.Token1Symbol);
                return false;
            }

            var tradePairGrain = _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(pair.Id));
            var existPairResultDto = await tradePairGrain.GetAsync();

            if (existPairResultDto.Success)
            {
                return true;
            }
            
            // await _revertProvider.CheckOrAddUnconfirmedTransaction(currentConfirmedHeight, EventType.TradePairEvent, pair.ChainId, pair.BlockHeight, pair.TransactionHash);

            var grain = _clusterClient.GetGrain<ITokenPathGrain>(chain.Id);
            var clearCountResultDto = await grain.ResetCacheAsync();
            Log.Information($"clear swap path cache, token path grain: {grain.GetPrimaryKeyString()}, count: {clearCountResultDto.Data}");
            
            var routeGrain = _clusterClient.GetGrain<IRouteGrain>(chain.Id);
            var clearRouteCountResultDto = await routeGrain.ResetCacheAsync();
            Log.Information($"clear route cache, route grain: {routeGrain.GetPrimaryKeyString()}, count: {clearRouteCountResultDto.Data}");
            
            var token0 = await _tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = chain.Id,
                Symbol = pair.Token0Symbol,
                Id = pair.Token0Id
            });
            var token1 = await _tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = chain.Id,
                Symbol = pair.Token1Symbol,
                Id = pair.Token1Id
            });

            if (token0 == null)
            {
                Log.Error("can not find token {token0Symbol}, chainId: {chainId}, pairId: {pairId}",
                    pair.Token0Symbol, chain.Id, pair.Id);
            }

            if (token1 == null)
            {
                Log.Error("can not find token {token1Symbol}, chainId:{chainId}, pairId:{pairId}",
                    pair.Token1Symbol, chain.Id, pair.Id);
            }

            if (token0 == null || token1 == null) return false;

            var grainDto = _objectMapper.Map<TradePairInfoDto, TradePairGrainDto>(pair);
            grainDto.Token0 = token0;
            grainDto.Token1 = token1;
            
            await tradePairGrain.AddOrUpdateAsync(grainDto);
            
            var chainTradePairsGrain = _clusterClient.GetGrain<IChainTradePairsGrain>(chain.Id);
            await chainTradePairsGrain.AddOrUpdateAsync(new ChainTradePairsGrainDto()
            {
                TradePairAddress = pair.Address,
                TradePairGrainId = tradePairGrain.GetPrimaryKeyString()
            });
            
            Log.Information("create pair success Id: {pairId}, chainId: {chainId}, token0: {token0}," +
                                   "token1:{token1}", pair.Id, chain.Id, pair.Token0Symbol, pair.Token1Symbol);

            await CreateTradePairIndexAsync(pair, token0, token1, chain);
            return true;
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
                .WithNotDeleted()
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

            var sorting = GetSortFunction(input.Sorting, input.Page);
            var list = await _tradePairIndexRepository.GetSortListAsync(Filter,
                sortFunc: sorting,
                limit: input.MaxResultCount == 0 ? TradePairConst.MaxPageSize :
                input.MaxResultCount > TradePairConst.MaxPageSize ? TradePairConst.MaxPageSize : input.MaxResultCount,
                skip: input.SkipCount);

            var totalCount = await _tradePairIndexRepository.CountAsync(Filter);

            var items = ObjectMapper.Map<List<Index.TradePair>, List<TradePairIndexDto>>(list.Item2);

           
            items = await AddFavoriteInfoAsync(items, input);

            return new PagedResultDto<TradePairIndexDto>
            {
                Items = items,
                TotalCount = totalCount.Count
            };
        }

        [ExceptionHandler(typeof(Exception), Message = "AddFavoriteInfo Error", 
            LogLevel = LogLevel.Error, TargetType = typeof(HandlerExceptionService), MethodName = nameof(HandlerExceptionService.HandleWithReturn))]
        protected virtual async Task<List<TradePairIndexDto>> AddFavoriteInfoAsync(List<TradePairIndexDto> inTradePairIndexDtos,
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
            mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));
            QueryContainer Filter(QueryContainerDescriptor<Index.TradePair> f) => f.Bool(b => b.Must(mustQuery));
            return await _tradePairIndexRepository.GetAsync(Filter);
        }
        
        private async Task<List<Index.TradePair>> GetListAsync(string chainId, List<string> transactionHashs, int maxResultCount)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<Index.TradePair>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.TransactionHash).Terms(transactionHashs)));
            
            QueryContainer Filter(QueryContainerDescriptor<Index.TradePair> f) => f.Bool(b => b.Must(mustQuery));
            var list = await _tradePairIndexRepository.GetListAsync(Filter, limit: maxResultCount);
            return list.Item2;
        }

        private async Task<bool> IsNeedUpdateAsync(TradePairGrainDto pair, DateTime time)
        {
            var lastSnapshot =
                await _tradePairMarketDataProvider.GetLatestTradePairMarketDataFromGrainAsync(pair.ChainId,
                    pair.Id);
            return lastSnapshot != null && lastSnapshot.Timestamp < time.AddHours(-1);
        }
        
        private static Func<SortDescriptor<Index.TradePair>, IPromise<IList<ISort>>> GetDefaultSort(TradePairPage page)
        {
            switch (page)
            {
                case TradePairPage.MarketPage:
                    return descriptor => descriptor.Descending(f => f.Volume24h).Descending(f => f.TVL)
                        .Descending(f => f.Price)
                        .Descending(f => f.FeeRate);
                    ;
                case TradePairPage.TradePage:
                    return descriptor => descriptor.Ascending(f => f.FeeRate);
                default:
                    return descriptor => descriptor.Ascending(f => f.Token0.Symbol);
                    ;
            }
        }

        private static Func<SortDescriptor<Index.TradePair>, IPromise<IList<ISort>>> GetSortFunction(string sorting,
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