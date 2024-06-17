using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.MyPortfolio;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Index;
using Microsoft.Extensions.Logging;
using Nest;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Volo.Abp.ObjectMapping;
using JsonConvert = Newtonsoft.Json.JsonConvert;
using TradePair = AwakenServer.Trade.Index.TradePair;
using TradePairMarketDataSnapshot = AwakenServer.Trade.Index.TradePairMarketDataSnapshot;


namespace AwakenServer.Asset;

[RemoteService(false)]
public class MyPortfolioAppService : ApplicationService, IMyPortfolioAppService
{
    public const string SyncedTransactionCachePrefix = "MyPortfolioSyned";
    private readonly IClusterClient _clusterClient;
    private readonly INESTRepository<TradePair, Guid> _tradePairIndexRepository;
    private readonly INESTRepository<CurrentUserLiquidityIndex, Guid> _currentUserLiquidityIndexRepository;
    private readonly INESTRepository<UserLiquiditySnapshotIndex, Guid> _userLiduiditySnapshotIndexRepository;
    private readonly INESTRepository<TradePairMarketDataSnapshot, Guid> _tradePairSnapshotIndexRepository;
    private readonly ITokenPriceProvider _tokenPriceProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly IDistributedCache<string> _syncedTransactionIdCache;
    private readonly ILogger<AssetAppService> _logger;

    public MyPortfolioAppService(IClusterClient clusterClient, 
        INESTRepository<TradePair, Guid> tradePairIndexRepository, 
        INESTRepository<CurrentUserLiquidityIndex, Guid> currentUserLiquidityIndexRepository,
        INESTRepository<UserLiquiditySnapshotIndex, Guid> userLiduiditySnapshotIndexRepository,
        INESTRepository<TradePairMarketDataSnapshot, Guid> tradePairSnapshotIndexRepository,
        IObjectMapper objectMapper,
        ITokenPriceProvider tokenPriceProvider,
        ILogger<AssetAppService> logger)
    {
        _clusterClient = clusterClient;
        _tradePairIndexRepository = tradePairIndexRepository;
        _currentUserLiquidityIndexRepository = currentUserLiquidityIndexRepository;
        _objectMapper = objectMapper;
        _userLiduiditySnapshotIndexRepository = userLiduiditySnapshotIndexRepository;
        _tradePairSnapshotIndexRepository = tradePairSnapshotIndexRepository;
        _tokenPriceProvider = tokenPriceProvider;
        _logger = logger;
    }

    public async Task<bool> SyncLiquidityRecordAsync(LiquidityRecordDto liquidityRecordDto)
    {
        var key = $"{SyncedTransactionCachePrefix}:{liquidityRecordDto.TransactionHash}";
        var existed = await _syncedTransactionIdCache.GetAsync(key);
        if (!existed.IsNullOrWhiteSpace())
        {
            return false;
        }
        var tradePair = await GetTradePairAsync(liquidityRecordDto.ChainId, liquidityRecordDto.Pair);
        if (tradePair == null)
        {
            return false;
        }
        var currentTradePairGrain = _clusterClient.GetGrain<ICurrentTradePairGrain>(GrainIdHelper.GenerateGrainId(tradePair.Id));
        await currentTradePairGrain.AddTotalSupplyAsync(liquidityRecordDto.Type == LiquidityType.Mint ? 
            liquidityRecordDto.LpTokenAmount : -liquidityRecordDto.LpTokenAmount);
        
        var currentUserLiquidityGrain = _clusterClient.GetGrain<ICurrentUserLiquidityGrain>(GrainIdHelper.GenerateGrainId(liquidityRecordDto.Address, tradePair.Id));
        var currentUserLiquidityGrainResult = liquidityRecordDto.Type == LiquidityType.Mint
            ? await currentUserLiquidityGrain.AddLiquidityAsync(tradePair, liquidityRecordDto)
            : await currentUserLiquidityGrain.RemoveLiquidityAsync(tradePair, liquidityRecordDto);
        // publish eto
        var userLiquiditySnapshotGrainDto = new UserLiquiditySnapshotGrainDto()
        {
            Address = liquidityRecordDto.Address,
            TradePairId = tradePair.Id,
            LpTokenAmount = currentUserLiquidityGrainResult.Data.LpTokenAmount,
            SnapShotTime = currentUserLiquidityGrainResult.Data.LastUpdateTime.Date
        };
        var userLiquiditySnapshotGrain = _clusterClient.GetGrain<IUserLiquiditySnapshotGrain>(
            GrainIdHelper.GenerateGrainId(liquidityRecordDto.Address, tradePair.Id, userLiquiditySnapshotGrainDto.SnapShotTime));
        await userLiquiditySnapshotGrain.AddOrUpdateAsync(userLiquiditySnapshotGrainDto);
        // publish eto
        await _syncedTransactionIdCache.SetAsync(key, "1");
        return true;
    }
    
    public async Task<bool> SyncSwapRecordAsync(SwapRecordDto swapRecordDto)
    {
        var key = $"{SyncedTransactionCachePrefix}:{swapRecordDto.TransactionHash}";
        var existed = await _syncedTransactionIdCache.GetAsync(key);
        if (!existed.IsNullOrWhiteSpace())
        {
            return false;
        }
        await SyncSingleSwapRecordAsync(swapRecordDto);
        if (!swapRecordDto.SwapRecords.IsNullOrEmpty())
        {
            foreach (var swapRecord in swapRecordDto.SwapRecords)
            {
                ObjectMapper.Map(swapRecord, swapRecordDto);
                await SyncSingleSwapRecordAsync(swapRecordDto);
            }
        }
        await _syncedTransactionIdCache.SetAsync(key, "1");
        return true;
    }

    public async Task<bool> SyncSingleSwapRecordAsync(SwapRecordDto swapRecordDto)
    {
        var tradePair = await GetTradePairAsync(swapRecordDto.ChainId, swapRecordDto.PairAddress);
        if (tradePair == null)
        {
            return false;
        }
        var currentTradePairGrain = _clusterClient.GetGrain<ICurrentTradePairGrain>(GrainIdHelper.GenerateGrainId(tradePair.Id));
        var isToken0 = swapRecordDto.SymbolIn == tradePair.Token0.Symbol;
        var total0Fee = isToken0 ? swapRecordDto.TotalFee : 0;
        var total1Fee = isToken0 ? 0 : swapRecordDto.TotalFee;
        var currentTradePairResult = await currentTradePairGrain.AddTotalFeeAsync(total0Fee, total1Fee);

        var userLiquidityList = await GetCurrentUserLiquidityIndexListAsync(tradePair.Id);
        foreach (var userLiquidity in userLiquidityList)
        {
            var userToken0Fee = total0Fee * userLiquidity.LpTokenAmount / currentTradePairResult.Data.TotalSupply;
            var userToken1Fee = total1Fee * userLiquidity.LpTokenAmount / currentTradePairResult.Data.TotalSupply;
            if (userToken0Fee == 0 && userToken1Fee == 0)
            {
                continue;
            }
            var currentLiquidityGrain = _clusterClient.GetGrain<ICurrentUserLiquidityGrain>(GrainIdHelper.GenerateGrainId(userLiquidity.Address, tradePair.Id));
            var currentLiquidityGrainResult = await currentLiquidityGrain.AddTotalFee(userToken0Fee, userToken1Fee);
            // publish CurrentUserLiquidityEto
            
            var userLiquiditySnapshotGrainDto = new UserLiquiditySnapshotGrainDto()
            {
                Address = userLiquidity.Address,
                TradePairId = tradePair.Id,
                LpTokenAmount = currentLiquidityGrainResult.Data.LpTokenAmount,
                SnapShotTime = currentLiquidityGrainResult.Data.LastUpdateTime.Date,
                Token0TotalFee = userToken0Fee,
                Token1TotalFee = userToken1Fee
            };
            var snapshotGrain = _clusterClient.GetGrain<IUserLiquiditySnapshotGrain>(
                GrainIdHelper.GenerateGrainId(userLiquidity.Address, tradePair.Id, currentLiquidityGrainResult.Data.LastUpdateTime.Date));
            await snapshotGrain.AddOrUpdateAsync(userLiquiditySnapshotGrainDto);
            // publish UserLiquiditySnapshotEto
        }
        return true;
    }


    public async Task<TradePair> GetTradePairAsync(string chainName, string address)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TradePair>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainName)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(address)));

        QueryContainer Filter(QueryContainerDescriptor<TradePair> f) => f.Bool(b => b.Must(mustQuery));
        return await _tradePairIndexRepository.GetAsync(Filter);
    }
    
    public async Task<List<CurrentUserLiquidityIndex>> GetCurrentUserLiquidityIndexListAsync(Guid tradePairId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CurrentUserLiquidityIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.TradePairId).Value(tradePairId)));
        mustQuery.Add(q => q.Range(i => i.Field(f => f.LpTokenAmount).GreaterThan(0)));
        QueryContainer Filter(QueryContainerDescriptor<CurrentUserLiquidityIndex> f) => f.Bool(b => b.Must(mustQuery));
        var result = await _currentUserLiquidityIndexRepository.GetListAsync(Filter, skip: 0, limit: 10000);
        return result.Item2;
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
        var list = await _currentUserLiquidityIndexRepository.GetListAsync(Filter);
        
        var sumValueInUsd = 0.0;
        var sumFeeInUsd = 0.0;
        var tokenDictionary = new Dictionary<string, TokenPortfolioInfoDto>();
        
        foreach (var userLiquidityIndex in list.Item2)
        {
            var tradePairGrain = _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(userLiquidityIndex.TradePairId));
            var pair = (await tradePairGrain.GetAsync()).Data;

            var lpTokenPercentage = String.IsNullOrEmpty(pair.TotalSupply) || pair.TotalSupply == "0"
                ? 0.0
                : userLiquidityIndex.LpTokenAmount / Double.Parse(pair.TotalSupply);
            var token0Percenage = pair.ValueLocked0 / (pair.ValueLocked0 + pair.ValueLocked1);
            var token1Percenage = pair.ValueLocked0 / (pair.ValueLocked0 + pair.ValueLocked1);
            var valueInUsd = lpTokenPercentage * pair.TVL;
            var fee = userLiquidityIndex.Token0UnReceivedFee + userLiquidityIndex.Token1UnReceivedFee;
            
            sumValueInUsd += valueInUsd;
            sumFeeInUsd += fee;
            
            result.TradePairDistributions.Add(new TradePairPortfolioDto()
            {
                TradePair = _objectMapper.Map<TradePairGrainDto, TradePairWithTokenDto>(pair),
                PositionInUsd = valueInUsd.ToString(),
                FeeInUsd = fee.ToString()
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
            pair.PositionPercent = sumValueInUsd != 0 ? (Double.Parse(pair.PositionInUsd) / sumValueInUsd).ToString() : "0";
            pair.FeePercent = sumFeeInUsd != 0 ? (Double.Parse(pair.FeeInUsd) / sumFeeInUsd).ToString() : "0";
        }

        foreach (var tokenPortfolio in tokenDictionary)
        {
            tokenPortfolio.Value.PositionPercent =
                sumValueInUsd != 0 ? (Double.Parse(tokenPortfolio.Value.PositionInUsd) / sumValueInUsd).ToString() : "0";
            tokenPortfolio.Value.FeePercent =
                sumFeeInUsd != 0 ? (Double.Parse(tokenPortfolio.Value.FeeInUsd) / sumFeeInUsd).ToString() : "0";
            result.TokenDistributions.Add(tokenPortfolio.Value);
        }

        result.TotalPositionsInUSD = sumValueInUsd.ToString();
        result.TotalFeeInUSD = sumFeeInUsd.ToString();
        
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
        var token0Price = await _tokenPriceProvider.GetTokenUSDPriceAsync(userLiquidityIndex.ChainId, token0Symbol);
        var token1Price = await _tokenPriceProvider.GetTokenUSDPriceAsync(userLiquidityIndex.ChainId, token1Symbol);
        
        if (type == EstimatedAprType.All)
        {
            var unReveivedFee = userLiquidityIndex.Token0UnReceivedFee * token0Price + userLiquidityIndex.Token1UnReceivedFee * token1Price;
            var cumulativeAddtion = userLiquidityIndex.Token0CumulativeAddition * token0Price +
                                    userLiquidityIndex.Token1CumulativeAddition * token1Price;
            var averageHoldingPeriod = GetAverageHoldingPeriod(userLiquidityIndex);
            if (cumulativeAddtion == 0 || averageHoldingPeriod == 0)
            {
                return 0.0;
            }
            _logger.LogInformation($"calculate EstimatedAPR input user address: {userLiquidityIndex.Address}, " +
                                   $"type: {type}, " +
                                   $"unReveivedFee: {unReveivedFee}, " +
                                   $"cumulativeAddtion: {cumulativeAddtion}," +
                                   $"averageHoldingPeriod: {averageHoldingPeriod}");
            
            return unReveivedFee / cumulativeAddtion / averageHoldingPeriod * 360 * 100;
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
            sumFee += (userLiquiditySnapshot.Token0TotalFee * token0Price + userLiquiditySnapshot.Token1TotalFee * token1Price);
        }

        var avgLpTokenInUsd = sumLpTokenInUsd / periodInDays;
        
        _logger.LogInformation($"calculate EstimatedAPR input user address: {userLiquidityIndex.Address}, " +
                               $"type: {type}, " +
                               $"sumFee: {sumFee}," +
                               $"avgLpTokenInUsd: {avgLpTokenInUsd}, " +
                               $"periodInDays: {periodInDays}");
        
        return avgLpTokenInUsd > 0 ? sumFee / periodInDays / avgLpTokenInUsd * 360 * 100 : 0;
        
    }
    
    public async Task<List<TradePairPositionDto>> ProcessUserPositionAsync(GetUserPositionsDto input, List<CurrentUserLiquidityIndex> userLiquidityIndices)
    {
        var result = new List<TradePairPositionDto>();
        
        foreach (var userLiquidityIndex in userLiquidityIndices)
        {
            var tradePairGrain = _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(userLiquidityIndex.TradePairId));
            var pair = (await tradePairGrain.GetAsync()).Data;

            _logger.LogInformation($"process user position input address: {input.Address}, user liquidity index: {JsonConvert.SerializeObject(userLiquidityIndex)}");
            
            var lpTokenPercentage = String.IsNullOrEmpty(pair.TotalSupply) || pair.TotalSupply == "0"
                ? 0.0
                : userLiquidityIndex.LpTokenAmount / Double.Parse(pair.TotalSupply);
            var token0Percenage = pair.ValueLocked0 / (pair.ValueLocked0 + pair.ValueLocked1);
            var token1Percenage = pair.ValueLocked0 / (pair.ValueLocked0 + pair.ValueLocked1);
            var valueInUsd = lpTokenPercentage * pair.TVL;
            var token0Price = await _tokenPriceProvider.GetTokenUSDPriceAsync(pair.ChainId, pair.Token0.Symbol);
            var token1Price = await _tokenPriceProvider.GetTokenUSDPriceAsync(pair.ChainId, pair.Token1.Symbol);
            var cumulativeAdditionInUsd = userLiquidityIndex.Token0CumulativeAddition * token0Price +
                                          userLiquidityIndex.Token1CumulativeAddition * token1Price;
            var averageHoldingPeriod = GetAverageHoldingPeriod(userLiquidityIndex);
            var estimatedAPR = await CalculateEstimatedAPRAsync(pair.Token0Symbol, pair.Token1Symbol, (EstimatedAprType)input.EstimatedAprType, userLiquidityIndex);
            var dynamicAPR = (averageHoldingPeriod != 0 && cumulativeAdditionInUsd != 0)
                ? (valueInUsd - cumulativeAdditionInUsd) / cumulativeAdditionInUsd * 360 /
                  averageHoldingPeriod
                : 0;
            
            _logger.LogInformation($"process user position input user address: {input.Address}, " +
                                   $"pair.Address: {pair.Address}, " +
                                   $"token0Price: {token0Price}, " +
                                   $"token1Price: {token1Price}, " +
                                   $"pair.TotalSupply: {pair.TotalSupply}, " +
                                   $"pair.ValueLocked0: {pair.ValueLocked0}, " +
                                   $"pair.ValueLocked1: {pair.ValueLocked1}, " +
                                   $"pair.TVL: {pair.TVL}, " +
                                   $"averageHoldingPeriod: {averageHoldingPeriod}," +
                                   $"lpTokenPercentage: {lpTokenPercentage}, " +
                                   $"valueInUsd: {valueInUsd}");

            _logger.LogInformation($"");
            
            result.Add(new TradePairPositionDto()
            {
                TradePairInfo = _objectMapper.Map<TradePairGrainDto, PositionTradePairDto>(pair),
                Token0Amount = (lpTokenPercentage * pair.ValueLocked0).ToString(),
                Token1Amount = (lpTokenPercentage * pair.ValueLocked1).ToString(),
                Token0Percent = token0Percenage.ToString(),
                Token1Percent = token1Percenage.ToString(),
                LpTokenAmount = userLiquidityIndex.LpTokenAmount.ToString(),
                Position = new LiquidityPoolValueInfo()
                {
                    ValueInUsd = valueInUsd.ToString(),
                    Token0Value = (lpTokenPercentage * pair.ValueLocked0).ToString(),
                    Token0ValueInUsd = (token0Percenage * valueInUsd).ToString(),
                    Token1Value = (lpTokenPercentage * pair.ValueLocked1).ToString(),
                    Token1ValueInUsd = (token1Percenage * valueInUsd).ToString(),
                },
                Fee = new LiquidityPoolValueInfo()
                {
                    ValueInUsd = (userLiquidityIndex.Token0UnReceivedFee * token0Price + userLiquidityIndex.Token1UnReceivedFee * token1Price).ToString(),
                    Token0Value = userLiquidityIndex.Token0UnReceivedFee.ToString(),
                    Token0ValueInUsd = (userLiquidityIndex.Token0UnReceivedFee * token0Price).ToString(),
                    Token1Value = userLiquidityIndex.Token1UnReceivedFee.ToString(),
                    Token1ValueInUsd = (userLiquidityIndex.Token1UnReceivedFee * token1Price).ToString(),
                },
                cumulativeAddition = new LiquidityPoolValueInfo()
                {
                    ValueInUsd = cumulativeAdditionInUsd.ToString(),
                    Token0Value = userLiquidityIndex.Token0CumulativeAddition.ToString(),
                    Token0ValueInUsd = (userLiquidityIndex.Token0CumulativeAddition * token0Price).ToString(),
                    Token1Value = userLiquidityIndex.Token1CumulativeAddition.ToString(),
                    Token1ValueInUsd = (userLiquidityIndex.Token1CumulativeAddition * token1Price).ToString(),
                },
                EstimatedAPRType = (EstimatedAprType)input.EstimatedAprType,
                EstimatedAPR = estimatedAPR.ToString(),
                ImpermanentLossInUSD = (valueInUsd - cumulativeAdditionInUsd).ToString(),
                DynamicAPR = dynamicAPR.ToString()
            });
        }

        return result;
    }
    
    public async Task<PagedResultDto<TradePairPositionDto>> GetUserPositionsAsync(GetUserPositionsDto input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CurrentUserLiquidityIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(input.Address)));
        QueryContainer Filter(QueryContainerDescriptor<CurrentUserLiquidityIndex> f) => f.Bool(b => b.Must(mustQuery));
        
        var list = await _currentUserLiquidityIndexRepository.GetListAsync(Filter);
        
        var userPositions = await ProcessUserPositionAsync(input, list.Item2);
        return new PagedResultDto<TradePairPositionDto>()
        {
            TotalCount = list.Item1,
            Items = userPositions
        };
    }
    
}