using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Client.MultiToken;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Chains;
using AwakenServer.Commons;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.Asset;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Price;
using AwakenServer.Provider;
using AwakenServer.Tokens;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Index;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Index = System.Index;
using TradePair = AwakenServer.Trade.TradePair;
using Volo.Abp.ObjectMapping;
using ITokenPriceProvider = AwakenServer.Trade.ITokenPriceProvider;
using TradePairMarketDataSnapshot = AwakenServer.Trade.Index.TradePairMarketDataSnapshot;

namespace AwakenServer.Asset;

[RemoteService(false)]
public class AssetAppService : ApplicationService, IAssetAppService
{
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly ITokenAppService _tokenAppService;
    private readonly IPriceAppService _priceAppService;
    private readonly AssetShowOptions _assetShowOptions;
    private readonly IAElfClientProvider _aelfClientProvider;
    private readonly AssetWhenNoTransactionOptions _assetWhenNoTransactionOptions;
    private readonly IDistributedCache<UserAssetInfoDto> _userAssetInfoDtoCache;
    private readonly INESTRepository<CurrentUserLiquidityIndex, Guid> _currentUserLiduidityIndexRepository;
    private readonly INESTRepository<UserLiquiditySnapshotIndex, Guid> _userLiduiditySnapshotIndexRepository;
    private readonly INESTRepository<TradePairMarketDataSnapshot, Guid> _tradePairSnapshotIndexRepository;
    private readonly ITokenPriceProvider _tokenPriceProvider;

    private readonly IObjectMapper _objectMapper;
    
    
    private const string userAssetInfoDtoPrefix = "AwakenServer:Asset:";
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<AssetAppService> _logger;

    public AssetAppService(IGraphQLProvider graphQlProvider,
        ITokenAppService tokenAppService,
        IPriceAppService priceAppService,
        IOptionsSnapshot<AssetShowOptions> optionsSnapshot,
        IAElfClientProvider aelfClientProvider,
        IOptionsSnapshot<AssetWhenNoTransactionOptions> showSymbolsWhenNoTransactionOptions,
        IDistributedCache<UserAssetInfoDto> userAssetInfoDtoCache, IClusterClient clusterClient,
        ILogger<AssetAppService> logger,
        INESTRepository<CurrentUserLiquidityIndex, Guid> currentUserLiduidityIndexRepository,
        INESTRepository<UserLiquiditySnapshotIndex, Guid> userLiduiditySnapshotIndexRepository,
        INESTRepository<TradePairMarketDataSnapshot, Guid> tradePairSnapshotIndexRepository,
        IObjectMapper objectMapper,
        ITokenPriceProvider tokenPriceProvider)
    {
        _graphQlProvider = graphQlProvider;
        _tokenAppService = tokenAppService;
        _priceAppService = priceAppService;
        _assetShowOptions = optionsSnapshot.Value;
        _aelfClientProvider = aelfClientProvider;
        _clusterClient = clusterClient;
        _assetWhenNoTransactionOptions = showSymbolsWhenNoTransactionOptions.Value;
        _userAssetInfoDtoCache = userAssetInfoDtoCache;
        _logger = logger;
        _objectMapper = objectMapper;
        _currentUserLiduidityIndexRepository = currentUserLiduidityIndexRepository;
        _userLiduiditySnapshotIndexRepository = userLiduiditySnapshotIndexRepository;
        _tradePairSnapshotIndexRepository = tradePairSnapshotIndexRepository;
        _tokenPriceProvider = tokenPriceProvider;
    }

    public async Task<UserAssetInfoDto> GetUserAssetInfoAsync(GetUserAssetInfoDto input)
    {
        var tokenList = await _graphQlProvider.GetUserTokensAsync(input.ChainId, input.Address);
        _logger.LogInformation("get user token list from indexer,symbol:{symbol}",
            tokenList.Select(s => s.Symbol).ToList());
        if (tokenList == null || tokenList.Count == 0)
        {
            return await GetAssetFromCacheOrAElfAsync(input.ChainId, input.Address);
        }

        var list = new List<UserTokenInfo>();

        var symbolList = tokenList.Select(i => i.Symbol).ToList();
        var symbolPriceMap =
            (await _priceAppService.GetTokenPriceListAsync(symbolList)).Items.ToDictionary(i => i.Symbol,
                i => i.PriceInUsd);
        foreach (var userTokenDto in tokenList)
        {
            var userTokenInfo = ObjectMapper.Map<UserTokenDto, UserTokenInfo>(userTokenDto);
            list.Add(userTokenInfo);

            await SetUserTokenInfoAsync(userTokenInfo, symbolPriceMap.GetValueOrDefault(userTokenDto.Symbol));
        }

        await AddNftTokenInfoAsync(list, input.ChainId, input.Address);


        return await FilterListAsync(list);
    }

    private async Task<UserAssetInfoDto> FilterListAsync(List<UserTokenInfo> list)
    {
        var showList = new List<UserTokenInfo>();
        showList = list.Where(o => o.PriceInUsd != null).Where(o => o.Balance > 0)
            .OrderByDescending(o => Double.Parse(o.PriceInUsd)).ThenBy(o => o.Symbol).ToList();

        return new UserAssetInfoDto()
        {
            ShowList = showList,
            HiddenList = new List<UserTokenInfo>()
        };
    }

    private async Task AddNftTokenInfoAsync(List<UserTokenInfo> list, string chainId, string address)
    {
        var userTokenInfosDic = list.ToDictionary(key => key.Symbol);
        foreach (var nftSymbol in _assetShowOptions.NftList)
        {
            if (!userTokenInfosDic.ContainsKey(nftSymbol))
            {
                var balanceOutput = await _aelfClientProvider.GetBalanceAsync(chainId, address,
                    _assetWhenNoTransactionOptions.ContractAddressOfGetBalance[chainId], nftSymbol);

                if (balanceOutput.Balance == 0)
                {
                    continue;
                }

                _logger.LogInformation("get balance,token:{token},balance:{balance}", nftSymbol, balanceOutput.Balance);
                if (balanceOutput != null)
                {
                    var userTokenInfo = new UserTokenInfo()
                    {
                        Balance = balanceOutput.Balance,
                        Symbol = balanceOutput.Symbol,
                        ChainId = chainId,
                        Address = address,
                    };
                    await SetUserTokenInfoAsync(userTokenInfo, 0);
                    list.Add(userTokenInfo);
                }
            }
        }
    }

    private async Task<UserAssetInfoDto> GetAssetFromCacheOrAElfAsync(string chainId, string address)
    {
        var symbolPriceMap =
            (await _priceAppService.GetTokenPriceListAsync(_assetWhenNoTransactionOptions.Symbols)).Items
            .ToDictionary(
                i => i.Symbol,
                i => i.PriceInUsd);

        var userAsset = await _userAssetInfoDtoCache.GetAsync($"{userAssetInfoDtoPrefix}:{chainId}:{address}");
        if (userAsset != null)
        {
            return userAsset;
        }


        var list = new List<UserTokenInfo>();
        foreach (var symbol in _assetWhenNoTransactionOptions.Symbols)
        {
            var balanceOutput = await _aelfClientProvider.GetBalanceAsync(chainId, address,
                _assetWhenNoTransactionOptions.ContractAddressOfGetBalance[chainId], symbol);
            if (balanceOutput != null)
            {
                var userTokenInfo = new UserTokenInfo()
                {
                    Balance = balanceOutput.Balance,
                    Symbol = balanceOutput.Symbol,
                    ChainId = chainId,
                    Address = address
                };
                list.Add(userTokenInfo);

                await SetUserTokenInfoAsync(userTokenInfo, symbolPriceMap.GetValueOrDefault(userTokenInfo.Symbol));
            }
        }


        await AddNftTokenInfoAsync(list, chainId, address);

        var result = await FilterListAsync(list);


        await _userAssetInfoDtoCache.SetAsync($"{userAssetInfoDtoPrefix}:{chainId}:{address}", result,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration =
                    DateTimeOffset.UtcNow.AddMinutes(_assetWhenNoTransactionOptions.ExpireDurationMinutes)
            });
        return result;
    }


    public async Task SetUserTokenInfoAsync(UserTokenInfo userTokenInfo, decimal priceInUsd)
    {
        var tokenDecimal = await GetTokenDecimalAsync(userTokenInfo.Symbol, userTokenInfo.ChainId);


        userTokenInfo.Amount = userTokenInfo.Balance.ToDecimalsString(tokenDecimal);


        userTokenInfo.PriceInUsd =
            ((long)(userTokenInfo.Balance * priceInUsd))
            .ToDecimalsString(tokenDecimal);
    }

    public async Task<int> GetTokenDecimalAsync(string symbol, string chainId)
    {
        var tokenDto = await _tokenAppService.GetAsync(new GetTokenInput
        {
            Symbol = symbol
        });

        if (tokenDto != null)
        {
            return tokenDto.Decimals;
        }


        var tokenInfo =
            await _aelfClientProvider.GetTokenInfoAsync(chainId, null, symbol);
        if (tokenInfo == null)
        {
            _logger.LogInformation("GetTokenInfo is null:{token}", symbol);
            return 0;
        }


        await _tokenAppService.CreateAsync(new TokenCreateDto
        {
            Symbol = symbol,
            Address = tokenInfo.Address,
            Decimals = tokenInfo.Decimals,
            ChainId = chainId
        });


        return tokenInfo.Decimals;
    }

    public async Task<TransactionFeeDto> GetTransactionFeeAsync()
    {
        return new TransactionFeeDto
        {
            TransactionFee = _assetShowOptions.TransactionFee
        };
    }

    public async Task<CommonResponseDto<Empty>> SetDefaultTokenAsync(SetDefaultTokenDto input)
    {
        try
        {
            if (!_assetShowOptions.ShowList.Exists(o => o == input.TokenSymbol))
            {
                throw new ArgumentException("no support symbol", input.TokenSymbol);
            }

            var defaultTokenGrain = _clusterClient.GetGrain<IDefaultTokenGrain>(input.Address);

            await defaultTokenGrain.SetTokenAsync(input.TokenSymbol);
            return new CommonResponseDto<Empty>();
        }
        catch (Exception e)
        {
            return new CommonResponseDto<Empty>().Error(e);
        }
    }


    public async Task<DefaultTokenDto> GetDefaultTokenAsync(GetDefaultTokenDto input)
    {
        var defaultTokenGrain = _clusterClient.GetGrain<IDefaultTokenGrain>(input.Address);

        var result = defaultTokenGrain.GetAsync();

        var defaultTokenDto = new DefaultTokenDto();
        defaultTokenDto.TokenSymbol = result.Result.Data.TokenSymbol ?? _assetShowOptions.DefaultSymbol;
        defaultTokenDto.Address = input.Address;
        return defaultTokenDto;
    }

    public async Task<UserPortfolioDto> GetUserPortfolioAsync(GetUserPortfolioDto input)
    {
        var result = new UserPortfolioDto()
        {
            TradePairDistributions = new List<TradePairPortfolioDto>(),
            TokenDistributions = new List<TokenPortfolioInfoDto>()
        };
        
        var mustQuery = new List<Func<QueryContainerDescriptor<CurrentUserLiquidityIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(input.Address)));
        QueryContainer Filter(QueryContainerDescriptor<CurrentUserLiquidityIndex> f) => f.Bool(b => b.Must(mustQuery));
        var list = await _currentUserLiduidityIndexRepository.GetListAsync(Filter);

        var sumValueInUsd = 0.0;
        var sumFeeInUsd = 0.0;
        var tokenDictionary = new Dictionary<string, TokenPortfolioInfoDto>();
        
        foreach (var userLiquidityIndex in list.Item2)
        {
            var tradePairGrain = _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(userLiquidityIndex.TradePairId));
            var pair = (await tradePairGrain.GetAsync()).Data;

            var lpTokenPercentage = String.IsNullOrEmpty(pair.TotalSupply) ? 0.0 : userLiquidityIndex.LpTokenAmount / Double.Parse(pair.TotalSupply);
            var token0Percenage = pair.ValueLocked0 / (pair.ValueLocked0 + pair.ValueLocked1);
            var token1Percenage = pair.ValueLocked0 / (pair.ValueLocked0 + pair.ValueLocked1);
            var valueInUsd = lpTokenPercentage * pair.TVL;
            var fee = userLiquidityIndex.Token0UnReceivedFee + userLiquidityIndex.Token1UnReceivedFee;
            
            sumValueInUsd += valueInUsd;
            sumFeeInUsd += fee;
            
            result.TradePairDistributions.Add(new TradePairPortfolioDto()
            {
                TradePair = _objectMapper.Map<TradePairGrainDto, TradePairWithTokenDto>(pair),
                PositionInUsd = valueInUsd,
                FeeInUsd = fee
            });

            if (!tokenDictionary.ContainsKey(pair.Token0.Symbol))
            {
                tokenDictionary.Add(pair.Token0.Symbol, new TokenPortfolioInfoDto()
                {
                    Token = pair.Token0
                });
            }
            
            if (!tokenDictionary.ContainsKey(pair.Token1.Symbol))
            {
                tokenDictionary.Add(pair.Token1.Symbol, new TokenPortfolioInfoDto()
                {
                    Token = pair.Token1
                });
            }

            tokenDictionary[pair.Token0.Symbol].PositionInUsd += token0Percenage * valueInUsd;
            tokenDictionary[pair.Token1.Symbol].PositionInUsd += token1Percenage * valueInUsd;
            tokenDictionary[pair.Token0.Symbol].FeeInUsd += token0Percenage * fee;
            tokenDictionary[pair.Token1.Symbol].FeeInUsd += token1Percenage * fee;
        }

        foreach (var pair in result.TradePairDistributions)
        {
            pair.PositionPercent = sumValueInUsd != 0 ? pair.PositionInUsd / sumValueInUsd : 0;
            pair.FeePercent = sumFeeInUsd != 0 ? pair.FeeInUsd / sumFeeInUsd : 0;
        }

        foreach (var tokenPortfolio in tokenDictionary)
        {
            tokenPortfolio.Value.PositionPercent =
                sumValueInUsd != 0 ? tokenPortfolio.Value.PositionInUsd / sumValueInUsd : 0;
            tokenPortfolio.Value.FeePercent =
                sumFeeInUsd != 0 ? tokenPortfolio.Value.FeeInUsd / sumFeeInUsd : 0;
            result.TokenDistributions.Add(tokenPortfolio.Value);
        }

        return result;
    }

    public long GetAverageHoldingPeriod(CurrentUserLiquidityIndex userLiquidityIndex)
    {
        return (DateTimeHelper.ToUnixTimeSeconds(DateTime.UtcNow) - userLiquidityIndex.AverageHoldingStartTime) / 24 * 60 * 60;
    }
    
    public long GetDayDifference(EstimatedAprType type, CurrentUserLiquidityIndex userLiquidityIndex)
    {
        switch (type)
        {
            case EstimatedAprType.Week:
            {
                return 7;
            }
            case EstimatedAprType.Month:
            {
                return 30;
            }
            case EstimatedAprType.All:
            {
                return GetAverageHoldingPeriod(userLiquidityIndex);
            }
            default:
            {
                return 7;
            }
        }
    }
    
    public async Task<List<UserLiquiditySnapshotIndex>> GetIndexListAsync(string chainId, Guid tradePairId,
        DateTime? timestampMin = null, DateTime? timestampMax = null)
    {
        var mustQuery =
            new List<Func<QueryContainerDescriptor<UserLiquiditySnapshotIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.TradePairId).Value(tradePairId)));

        if (timestampMin != null)
        {
            mustQuery.Add(q => q.DateRange(i =>
                i.Field(f => f.SnapShotTime)
                    .GreaterThan(timestampMin.Value)));
        }

        if (timestampMax != null)
        {
            mustQuery.Add(q => q.DateRange(i =>
                i.Field(f => f.SnapShotTime)
                    .LessThanOrEquals(timestampMax)));
        }

        QueryContainer Filter(QueryContainerDescriptor<UserLiquiditySnapshotIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var list = await _userLiduiditySnapshotIndexRepository.GetListAsync(Filter);
        return list.Item2;
    }

    private async Task<double> GetLpTokenSnapshotValueAsync(string token0Symbol, string token1Symbol, UserLiquiditySnapshotIndex userLiquiditySnapshotIndex)
    {
        var mustQuery =
            new List<Func<QueryContainerDescriptor<TradePairMarketDataSnapshot>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(userLiquiditySnapshotIndex.ChainId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.TradePairId).Value(userLiquiditySnapshotIndex.TradePairId)));
        mustQuery.Add(q => q.DateRange(i =>
            i.Field(f => f.Timestamp)
                .LessThanOrEquals(userLiquiditySnapshotIndex.SnapShotTime)));
        
        QueryContainer Filter(QueryContainerDescriptor<TradePairMarketDataSnapshot> f) =>
            f.Bool(b => b.Must(mustQuery));

        var snapshot = await _tradePairSnapshotIndexRepository.GetAsync(Filter, sortType: SortOrder.Descending, sortExp: o => o.Timestamp);
        
        var token0PriceInUsd = await _tokenPriceProvider.GetTokenUSDPriceAsync(snapshot.ChainId, token0Symbol);
        var token1PriceInUsd = await _tokenPriceProvider.GetTokenUSDPriceAsync(snapshot.ChainId, token1Symbol);
        return snapshot.ValueLocked0 * token0PriceInUsd + snapshot.ValueLocked1 * token1PriceInUsd;
    }

    private async Task<double> CalculateEstimatedAPRAsync(string token0Symbol, string token1Symbol, EstimatedAprType type, CurrentUserLiquidityIndex userLiquidityIndex)
    {
        if (type == EstimatedAprType.All)
        {
            var unReveivedFee = userLiquidityIndex.Token0UnReceivedFee + userLiquidityIndex.Token1UnReceivedFee;
            var cumulativeAddtion = userLiquidityIndex.Token0CumulativeAddition +
                                    userLiquidityIndex.Token1CumulativeAddition; // todo convert to usd
            return unReveivedFee / cumulativeAddtion / GetAverageHoldingPeriod(userLiquidityIndex) * 360 * 100;
        }
       
        // 7d, 30d
        var periodInDays = GetDayDifference(type, userLiquidityIndex);
        var userLiquiditySnapshots = await GetIndexListAsync(userLiquidityIndex.ChainId, userLiquidityIndex.TradePairId,
            DateTime.Now.AddDays(-periodInDays));
    
        var sumLpTokenInUsd = 0.0;
        var sumFee = 0.0;
        foreach (var userLiquiditySnapshot in userLiquiditySnapshots)
        {
            var lpTokenValueInUsd = await GetLpTokenSnapshotValueAsync(token0Symbol, token1Symbol, userLiquiditySnapshot);
            sumLpTokenInUsd += lpTokenValueInUsd;
            sumFee += (userLiquiditySnapshot.Token0TotalFee + userLiquiditySnapshot.Token1TotalFee);
        }

        var avgLpTokenInUsd = sumLpTokenInUsd / periodInDays;
        return avgLpTokenInUsd > 0 ? sumFee / periodInDays / avgLpTokenInUsd * 360 * 100 : 0;
        
    }
    
    public async Task<UserPositionsDto> ProcessUserPositionAsync(GetUserPositionsDto input, List<CurrentUserLiquidityIndex> userLiquidityIndices)
    {
        var result = new UserPositionsDto()
        {
            Address = input.Address,
            TradePairPositions = new List<TradePairPositionDto>()
        };
        
        foreach (var userLiquidityIndex in userLiquidityIndices)
        {
            var tradePairGrain = _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(userLiquidityIndex.TradePairId));
            var pair = (await tradePairGrain.GetAsync()).Data;

            var lpTokenPercentage = String.IsNullOrEmpty(pair.TotalSupply) ? 0.0 : userLiquidityIndex.LpTokenAmount / Double.Parse(pair.TotalSupply);
            var token0Percenage = pair.ValueLocked0 / (pair.ValueLocked0 + pair.ValueLocked1);
            var token1Percenage = pair.ValueLocked0 / (pair.ValueLocked0 + pair.ValueLocked1);
            var valueInUsd = lpTokenPercentage * pair.TVL;
            
            var estimatedAPR = await CalculateEstimatedAPRAsync(pair.Token0Symbol, pair.Token1Symbol, (EstimatedAprType)input.EstimatedAprType, userLiquidityIndex);
            
            result.TradePairPositions.Add(new TradePairPositionDto()
            {
                TradePairInfo = _objectMapper.Map<TradePairGrainDto, PositionTradePairDto>(pair),
                Token0Amount = lpTokenPercentage * pair.ValueLocked0,
                Token1Amount = lpTokenPercentage * pair.ValueLocked1,
                Token0Percent = token0Percenage,
                Token1Percent = token1Percenage,
                LpTokenAmount = userLiquidityIndex.LpTokenAmount,
                Position = new LiquidityPoolValueInfo()
                {
                    ValueInUsd = valueInUsd,
                    Token0ValueInUsd = token0Percenage * valueInUsd,
                    Token1ValueInUsd = token1Percenage * valueInUsd,
                },
                Fee = new LiquidityPoolValueInfo()
                {
                    ValueInUsd = userLiquidityIndex.Token0UnReceivedFee + userLiquidityIndex.Token1UnReceivedFee,
                    Token0ValueInUsd = userLiquidityIndex.Token0UnReceivedFee,
                    Token1ValueInUsd = userLiquidityIndex.Token1UnReceivedFee,
                },
                cumulativeAddition = new LiquidityPoolValueInfo()
                {
                    ValueInUsd = userLiquidityIndex.Token0CumulativeAddition + userLiquidityIndex.Token1CumulativeAddition,
                    Token0ValueInUsd = userLiquidityIndex.Token0CumulativeAddition,
                    Token1ValueInUsd = userLiquidityIndex.Token1CumulativeAddition,
                },
                EstimatedAPRType = (EstimatedAprType)input.EstimatedAprType,
                EstimatedAPR = estimatedAPR,
                ImpermanentLossInUSD = valueInUsd - (userLiquidityIndex.Token0CumulativeAddition + userLiquidityIndex.Token1CumulativeAddition), // todo convert to usd
                DynamicAPR = 0 // todo
            });
        }

        return result;
    }
    
    public async Task<UserPositionsDto> GetUserPositionsAsync(GetUserPositionsDto input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CurrentUserLiquidityIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(input.Address)));
        QueryContainer Filter(QueryContainerDescriptor<CurrentUserLiquidityIndex> f) => f.Bool(b => b.Must(mustQuery));
            
        var list = await _currentUserLiduidityIndexRepository.GetListAsync(Filter);

        return await ProcessUserPositionAsync(input, list.Item2);
    }
    
    public async Task<IdleTokensDto> GetIdleTokensAsync(GetIdleTokensDto input)
    {
        var tokenListDto = await GetUserAssetInfoAsync(new GetUserAssetInfoDto()
        {
            ChainId = input.ChainId,
            Address = input.Address
        });

        var totalValueInUsd = 0.0;
        foreach (var userTokenInfo in tokenListDto.ShowList)
        {
            totalValueInUsd += Double.Parse(userTokenInfo.PriceInUsd);
        }
        
        var idleTokenList = new List<IdleToken>();
        foreach (var userTokenInfo in tokenListDto.ShowList)
        {
            var percent = totalValueInUsd != 0.0 ? Double.Parse(userTokenInfo.PriceInUsd) / totalValueInUsd : 0.0;
            var tokenDto = await _tokenAppService.GetAsync(new GetTokenInput
            {
                Symbol = userTokenInfo.Symbol
            });
            idleTokenList.Add(new IdleToken()
            {
                Percent = percent.ToString(),
                ValueInUsd = userTokenInfo.PriceInUsd,
                TokenDto = tokenDto
            });
        }

        return new IdleTokensDto()
        {
            TotalValueInUsd = totalValueInUsd.ToString(),
            IdleTokens = idleTokenList
        };
    }
}