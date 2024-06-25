using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.MyPortfolio;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Tokens;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Etos;
using AwakenServer.Trade.Index;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Linq;
using Nest;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Volo.Abp.ObjectMapping;
using JsonConvert = Newtonsoft.Json.JsonConvert;
using Volo.Abp.EventBus.Distributed;
using TradePair = AwakenServer.Trade.Index.TradePair;
using TradePairMarketDataSnapshot = AwakenServer.Trade.Index.TradePairMarketDataSnapshot;


namespace AwakenServer.Asset;

[RemoteService(false)]
public class MyPortfolioAppService : ApplicationService, IMyPortfolioAppService
{
    public const string SyncedTransactionCachePrefix = "MyPortfolioSynced";
    private readonly IClusterClient _clusterClient;
    private readonly INESTRepository<TradePair, Guid> _tradePairIndexRepository;
    private readonly INESTRepository<CurrentUserLiquidityIndex, Guid> _currentUserLiquidityIndexRepository;
    private readonly INESTRepository<UserLiquiditySnapshotIndex, Guid> _userLiduiditySnapshotIndexRepository;
    private readonly INESTRepository<TradePairMarketDataSnapshot, Guid> _tradePairSnapshotIndexRepository;
    private readonly ITokenPriceProvider _tokenPriceProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IDistributedCache<string> _syncedTransactionIdCache;
    private readonly ILogger<MyPortfolioAppService> _logger;

    public MyPortfolioAppService(IClusterClient clusterClient, 
        INESTRepository<TradePair, Guid> tradePairIndexRepository, 
        INESTRepository<CurrentUserLiquidityIndex, Guid> currentUserLiquidityIndexRepository,
        INESTRepository<UserLiquiditySnapshotIndex, Guid> userLiduiditySnapshotIndexRepository,
        INESTRepository<TradePairMarketDataSnapshot, Guid> tradePairSnapshotIndexRepository,
        IObjectMapper objectMapper,
        ITokenPriceProvider tokenPriceProvider,
        IDistributedCache<string> syncedTransactionIdCache,
        IDistributedEventBus distributedEventBus,
        ILogger<MyPortfolioAppService> logger)
    {
        _clusterClient = clusterClient;
        _tradePairIndexRepository = tradePairIndexRepository;
        _currentUserLiquidityIndexRepository = currentUserLiquidityIndexRepository;
        _objectMapper = objectMapper;
        _userLiduiditySnapshotIndexRepository = userLiduiditySnapshotIndexRepository;
        _tradePairSnapshotIndexRepository = tradePairSnapshotIndexRepository;
        _tokenPriceProvider = tokenPriceProvider;
        _logger = logger;
        _syncedTransactionIdCache = syncedTransactionIdCache;
        _distributedEventBus = distributedEventBus;
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
            _logger.LogInformation("can not find trade pair: {chainId}, {pairAddress}", liquidityRecordDto.ChainId,
                liquidityRecordDto.Address);
            return false;
        }
        var currentTradePairGrain = _clusterClient.GetGrain<ICurrentTradePairGrain>(GrainIdHelper.GenerateGrainId(tradePair.Id));
        await currentTradePairGrain.AddTotalSupplyAsync(liquidityRecordDto.Type == LiquidityType.Mint ? 
            liquidityRecordDto.LpTokenAmount : -liquidityRecordDto.LpTokenAmount, liquidityRecordDto.Timestamp);
        
        var currentUserLiquidityGrain = _clusterClient.GetGrain<ICurrentUserLiquidityGrain>(GrainIdHelper.GenerateGrainId(liquidityRecordDto.Address, tradePair.Id));
        var currentUserLiquidityGrainResult = liquidityRecordDto.Type == LiquidityType.Mint
            ? await currentUserLiquidityGrain.AddLiquidityAsync(tradePair, liquidityRecordDto)
            : await currentUserLiquidityGrain.RemoveLiquidityAsync(tradePair, liquidityRecordDto);
        // publish eto
        await _distributedEventBus.PublishAsync(
            ObjectMapper.Map<CurrentUserLiquidity, CurrentUserLiquidityEto>(currentUserLiquidityGrainResult.Data));
        var userLiquiditySnapshotGrainDto = new UserLiquiditySnapshotGrainDto()
        {
            Address = liquidityRecordDto.Address,
            TradePairId = tradePair.Id,
            LpTokenAmount = currentUserLiquidityGrainResult.Data.LpTokenAmount,
            SnapShotTime = currentUserLiquidityGrainResult.Data.LastUpdateTime.Date
        };
        var userLiquiditySnapshotGrain = _clusterClient.GetGrain<IUserLiquiditySnapshotGrain>(
            GrainIdHelper.GenerateGrainId(liquidityRecordDto.Address, tradePair.Id, userLiquiditySnapshotGrainDto.SnapShotTime));
        var userLiquiditySnapshotResult = await userLiquiditySnapshotGrain.AddOrUpdateAsync(userLiquiditySnapshotGrainDto);
        // publish eto
        await _distributedEventBus.PublishAsync(ObjectMapper.Map<UserLiquiditySnapshot, UserLiquiditySnapshotEto>(userLiquiditySnapshotResult.Data));
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
            _logger.LogInformation("can not find trade pair: {chainId}, {pairAddress}", swapRecordDto.ChainId,
                swapRecordDto.PairAddress);
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
            var currentLiquidityGrainResult = await currentLiquidityGrain.AddTotalFee(userToken0Fee, userToken1Fee, swapRecordDto);
            // publish CurrentUserLiquidityEto
            await _distributedEventBus.PublishAsync(
                ObjectMapper.Map<CurrentUserLiquidity, CurrentUserLiquidityEto>(currentLiquidityGrainResult.Data));
            
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
            var snapshotResult = await snapshotGrain.AddOrUpdateAsync(userLiquiditySnapshotGrainDto);
            // publish UserLiquiditySnapshotEto
            await _distributedEventBus.PublishAsync(ObjectMapper.Map<UserLiquiditySnapshot, UserLiquiditySnapshotEto>(snapshotResult.Data));
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

    private List<TradePairPortfolioDto> MergeAndProcess(List<TradePairPortfolioDto> rawList, int showCount, double total)
    {
        showCount = showCount >= 1 ? showCount - 1 : 0;
        var result = new List<TradePairPortfolioDto>();
        
        var sortedPositionDistributions = rawList
            .Where(u => double.TryParse(u.ValueInUsd, out _))
            .OrderByDescending(u => double.Parse(u.ValueInUsd))
            .ToList();

        for (int i = 0; i < sortedPositionDistributions.Count; i++)
        {
            var pair = sortedPositionDistributions[i];
            pair.ValuePercent = total != 0 ? (Double.Parse(pair.ValueInUsd) / total * 100).ToString("F2") : "0";
            if (i < showCount)
            {
                pair.Name = $"{pair.TradePair.Token0.Symbol}/{pair.TradePair.Token1.Symbol}-{pair.TradePair.FeeRate}";
                result.Add(pair);
            }
            else if (i == showCount)
            {
                result.Add(new TradePairPortfolioDto()
                {
                    TradePair = new TradePairWithTokenDto()
                    {
                        ChainId = pair.TradePair.ChainId,
                    },
                    Name = "Other",
                    ValuePercent = pair.ValuePercent,
                    ValueInUsd = pair.ValueInUsd
                });
            }
            else
            {
                var sumValueInUsd = double.Parse(result[result.Count - 1].ValueInUsd) + double.Parse(pair.ValueInUsd);
                var sumPercent = double.Parse(result[result.Count - 1].ValuePercent) + double.Parse(pair.ValuePercent);
                result[result.Count - 1].ValueInUsd = sumValueInUsd.ToString();
                result[result.Count - 1].ValuePercent = sumPercent.ToString("F2");
            }
        }

        // Ensure the sum of all ValuePercent is exactly 100%
        double finalTotalPercent = result.Sum(r => double.Parse(r.ValuePercent));
        if (finalTotalPercent != 100.00)
        {
            double difference = 100.00 - finalTotalPercent;
            result[result.Count - 1].ValuePercent = (double.Parse(result[result.Count - 1].ValuePercent) + difference).ToString("F2");
        }

        
        return result;
    }
    
    private List<TokenPortfolioInfoDto> MergeAndProcess(Dictionary<string, TokenPortfolioInfoDto> rawList, int showCount, double total)
    {
        showCount = showCount >= 1 ? showCount - 1 : 0;
        var result = new List<TokenPortfolioInfoDto>();
        
        var sortedPositionDistributions = rawList
            .Where(u => double.TryParse(u.Value.ValueInUsd, out _))
            .OrderByDescending(u => double.Parse(u.Value.ValueInUsd))
            .ToList();
        
        for (int i = 0; i < sortedPositionDistributions.Count; i++)
        {
            var tokenInfoPair = sortedPositionDistributions[i];
            tokenInfoPair.Value.ValuePercent = total != 0 ? (Double.Parse(tokenInfoPair.Value.ValueInUsd) / total * 100).ToString("F2") : "0";
            if (i < showCount)
            {
                result.Add(tokenInfoPair.Value);
            }
            else if (i == showCount)
            {
                result.Add(new TokenPortfolioInfoDto()
                {
                    Token = new TokenDto()
                    {
                        ChainId = tokenInfoPair.Value.Token.ChainId,
                        Symbol = "Other",
                    },
                    ValuePercent = tokenInfoPair.Value.ValuePercent,
                    ValueInUsd = tokenInfoPair.Value.ValueInUsd
                });
            }
            else
            {
                var sumValueInUsd = double.Parse(result[result.Count - 1].ValueInUsd) + double.Parse(tokenInfoPair.Value.ValueInUsd);
                var sumPercent = double.Parse(result[result.Count - 1].ValuePercent) + double.Parse(tokenInfoPair.Value.ValuePercent);
                result[result.Count - 1].ValueInUsd = sumValueInUsd.ToString();
                result[result.Count - 1].ValuePercent = sumPercent.ToString("F2");
            }
        }
        
        // Ensure the sum of all ValuePercent is exactly 100%
        double finalTotalPercent = result.Sum(r => double.Parse(r.ValuePercent));
        if (finalTotalPercent != 100.00)
        {
            double difference = 100.00 - finalTotalPercent;
            result[result.Count - 1].ValuePercent = (double.Parse(result[result.Count - 1].ValuePercent) + difference).ToString("F2");
        }
        
        return result;
    }
    
    public async Task<UserPortfolioDto> GetUserPortfolioAsync(GetUserPortfolioDto input)
    {
        var positionDistributions = new List<TradePairPortfolioDto>();
        var feeDistributions = new List<TradePairPortfolioDto>();
        var mustQuery = new List<Func<QueryContainerDescriptor<CurrentUserLiquidityIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(input.Address)));
        QueryContainer Filter(QueryContainerDescriptor<CurrentUserLiquidityIndex> f) => f.Bool(b => b.Must(mustQuery));
        var list = await _currentUserLiquidityIndexRepository.GetListAsync(Filter);
        
        var sumValueInUsd = 0.0;
        var sumFeeInUsd = 0.0;
        var tokenPositionDictionary = new Dictionary<string, TokenPortfolioInfoDto>();
        var tokenFeeDictionary = new Dictionary<string, TokenPortfolioInfoDto>();
        
        foreach (var userLiquidityIndex in list.Item2)
        {
            var tradePairGrain = _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(userLiquidityIndex.TradePairId));
            var pair = (await tradePairGrain.GetAsync()).Data;
            var currentTradePairGrain = _clusterClient.GetGrain<ICurrentTradePairGrain>(GrainIdHelper.GenerateGrainId(userLiquidityIndex.TradePairId));
            var currentTradePair = (await currentTradePairGrain.GetAsync()).Data;
            
            var lpTokenPercentage = currentTradePair.TotalSupply == 0
                ? 0.0
                : userLiquidityIndex.LpTokenAmount / (double)currentTradePair.TotalSupply;
            
            var token0Price = await _tokenPriceProvider.GetTokenUSDPriceAsync(pair.ChainId, pair.Token0.Symbol);
            var token1Price = await _tokenPriceProvider.GetTokenUSDPriceAsync(pair.ChainId, pair.Token1.Symbol);
            var token0ValueInUsd = lpTokenPercentage * pair.ValueLocked0 * token0Price;
            var token1ValueInUsd = lpTokenPercentage * pair.ValueLocked1 * token1Price;
            var valueInUsd = lpTokenPercentage * pair.TVL;
            var token0Fee = Double.Parse(userLiquidityIndex.Token0UnReceivedFee.ToDecimalsString(pair.Token0.Decimals));
            var token1Fee = Double.Parse(userLiquidityIndex.Token1UnReceivedFee.ToDecimalsString(pair.Token1.Decimals));
            var fee = token0Fee + token1Fee;
            
            sumValueInUsd += valueInUsd;
            sumFeeInUsd += fee;
            
            positionDistributions.Add(new TradePairPortfolioDto()
            {
                TradePair = _objectMapper.Map<TradePairGrainDto, TradePairWithTokenDto>(pair),
                ValueInUsd = valueInUsd.ToString(),
            });
            feeDistributions.Add(new TradePairPortfolioDto()
            {
                TradePair = _objectMapper.Map<TradePairGrainDto, TradePairWithTokenDto>(pair),
                ValueInUsd = fee.ToString()
            });

            if (!tokenPositionDictionary.ContainsKey(pair.Token0.Symbol))
            {
                tokenPositionDictionary.Add(pair.Token0.Symbol, new TokenPortfolioInfoDto()
                {
                    Token = pair.Token0,
                    ValueInUsd = "0",
                    ValuePercent = "0"
                });
            }
            if (!tokenPositionDictionary.ContainsKey(pair.Token1.Symbol))
            {
                tokenPositionDictionary.Add(pair.Token1.Symbol, new TokenPortfolioInfoDto()
                {
                    Token = pair.Token1,
                    ValueInUsd = "0",
                    ValuePercent = "0"
                });
            }
            if (!tokenFeeDictionary.ContainsKey(pair.Token0.Symbol))
            {
                tokenFeeDictionary.Add(pair.Token0.Symbol, new TokenPortfolioInfoDto()
                {
                    Token = pair.Token0,
                    ValueInUsd = "0",
                    ValuePercent = "0"
                });
            }
            if (!tokenFeeDictionary.ContainsKey(pair.Token1.Symbol))
            {
                tokenFeeDictionary.Add(pair.Token1.Symbol, new TokenPortfolioInfoDto()
                {
                    Token = pair.Token1,
                    ValueInUsd = "0",
                    ValuePercent = "0"
                });
            }

            var currentToken0Position = double.Parse(tokenPositionDictionary[pair.Token0.Symbol].ValueInUsd) + token0ValueInUsd;
            tokenPositionDictionary[pair.Token0.Symbol].ValueInUsd = currentToken0Position.ToString();
            
            var currentToken1Position = double.Parse(tokenPositionDictionary[pair.Token1.Symbol].ValueInUsd) + token1ValueInUsd;
            tokenPositionDictionary[pair.Token1.Symbol].ValueInUsd = currentToken1Position.ToString();
            
            var currentToken0Fee = double.Parse(tokenFeeDictionary[pair.Token0.Symbol].ValueInUsd) + token0Fee;
            tokenFeeDictionary[pair.Token0.Symbol].ValueInUsd = currentToken0Fee.ToString();
            
            var currentToken1Fee = double.Parse(tokenFeeDictionary[pair.Token1.Symbol].ValueInUsd) + token1Fee;
            tokenFeeDictionary[pair.Token1.Symbol].ValueInUsd = currentToken1Fee.ToString();
        }
        
        return new UserPortfolioDto()
        {
            TotalPositionsInUSD = sumValueInUsd.ToString(),
            TotalFeeInUSD = sumFeeInUsd.ToString(),
            TradePairPositionDistributions = MergeAndProcess(positionDistributions, input.ShowCount, sumValueInUsd),
            TradePairFeeDistributions = MergeAndProcess(feeDistributions, input.ShowCount, sumFeeInUsd),
            TokenPositionDistributions = MergeAndProcess(tokenPositionDictionary, input.ShowCount, sumValueInUsd),
            TokenFeeDistributions = MergeAndProcess(tokenFeeDictionary, input.ShowCount, sumFeeInUsd),
        };
    }

    public long GetAverageHoldingPeriod(CurrentUserLiquidityIndex userLiquidityIndex)
    {
        return (DateTimeHelper.ToUnixTimeSeconds(DateTime.UtcNow) - DateTimeHelper.ToUnixTimeSeconds(userLiquidityIndex.AverageHoldingStartTime)) / (24 * 60 * 60);
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
    
    public async Task<List<UserLiquiditySnapshotIndex>> GetSnapshotIndexListAsync(string userAddress, string chainId, Guid tradePairId,
        long periodInDays)
    {
        var mustQuery =
            new List<Func<QueryContainerDescriptor<UserLiquiditySnapshotIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.TradePairId).Value(tradePairId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(userAddress)));
        var timestampMin = DateTime.UtcNow.AddDays(-periodInDays).Date;
        var timestampMax = DateTime.UtcNow.AddDays(-1).Date;
        mustQuery.Add(q => q.DateRange(i =>
            i.Field(f => f.SnapShotTime)
                .GreaterThanOrEquals(timestampMin)));
        mustQuery.Add(q => q.DateRange(i =>
            i.Field(f => f.SnapShotTime)
                .LessThanOrEquals(timestampMax)));
            
        QueryContainer Filter(QueryContainerDescriptor<UserLiquiditySnapshotIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var list = await _userLiduiditySnapshotIndexRepository.GetListAsync(Filter);
        if (list.Item1 == periodInDays || list.Item1 == 0)
        {
            return list.Item2;
        }

        var latestSnapshotIndex = await GetLatestUserLiquiditySnapshotIndexAsync(userAddress,
            chainId, tradePairId, timestampMin);
        var latestLpTokenAmount = latestSnapshotIndex?.LpTokenAmount ?? 0;
        for (var day = 0; day < periodInDays; day++)
        {
            var snapshotTime = timestampMin.AddDays(day);
            var snapshotIndex = list.Item2.FirstOrDefault(t => t.SnapShotTime == snapshotTime);
            if (snapshotIndex == null)
            {
                list.Item2.Add(new UserLiquiditySnapshotIndex
                {
                    ChainId = chainId,
                    TradePairId = tradePairId,
                    Address = userAddress,
                    LpTokenAmount = latestLpTokenAmount,
                    SnapShotTime = snapshotTime
                });
            }
            else
            {
                latestLpTokenAmount = snapshotIndex.LpTokenAmount;
            }
        }

        return list.Item2;
    }

    public async Task<UserLiquiditySnapshotIndex> GetLatestUserLiquiditySnapshotIndexAsync(string userAddress, string chainId,
        Guid tradePairId, DateTime timestampMax)
    {
        var mustQuery =
            new List<Func<QueryContainerDescriptor<UserLiquiditySnapshotIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.TradePairId).Value(tradePairId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(userAddress)));
        mustQuery.Add(q => q.DateRange(i =>
            i.Field(f => f.SnapShotTime)
                .LessThan(timestampMax)));
        QueryContainer Filter(QueryContainerDescriptor<UserLiquiditySnapshotIndex> f) =>
            f.Bool(b => b.Must(mustQuery));
        var list = await _userLiduiditySnapshotIndexRepository.GetListAsync(Filter, 
            sortExp:k=>k.SnapShotTime, sortType:SortOrder.Descending, skip:0, limit: 1);
        return list.Item1 > 0 ? list.Item2[0] : null;
    }

    private async Task<TradePairMarketDataSnapshot> GetTradePairMarketSnapshotAsync(
        UserLiquiditySnapshotIndex userLiquiditySnapshotIndex)
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
        return snapshot;
    }

    private async Task<double> CalculateEstimatedAPRAsync(
        EstimatedAprType type,
        int token0Decimal,
        int token1Decimal,
        double token0Price,
        double token1Price,
        CurrentUserLiquidityIndex userLiquidityIndex)
    {
        switch (type)
        {
            case EstimatedAprType.Week:
            case EstimatedAprType.Month:
            {
                var periodInDays = GetDayDifference(type, userLiquidityIndex);
                var userLiquiditySnapshots = await GetSnapshotIndexListAsync(userLiquidityIndex.Address,
                    userLiquidityIndex.ChainId, userLiquidityIndex.TradePairId,
                    periodInDays);

                _logger.LogInformation($"calculate EstimatedAPR input user address: {userLiquidityIndex.Address}, " +
                                       $"get snapshot from es begin, " +
                                       $"pair: {userLiquidityIndex.TradePairId}, " +
                                       $"snapshot count: {userLiquiditySnapshots.Count}");
                if (userLiquiditySnapshots.Count == 0)
                {
                    return 0.0;
                }

                var sumLpTokenInUsdBag = new ConcurrentBag<double>();
                var sumFeeBag = new ConcurrentBag<double>();
                var actualSnapshotCount = 0;
                var tasks = userLiquiditySnapshots.Select(async userLiquiditySnapshot =>
                {
                    var tradePairMarketSnapshot = await GetTradePairMarketSnapshotAsync(userLiquiditySnapshot);
                    if (tradePairMarketSnapshot != null)
                    {
                        ++actualSnapshotCount;
                        var tvl = tradePairMarketSnapshot.ValueLocked0 * token0Price + tradePairMarketSnapshot.ValueLocked1 * token1Price;
                        var lpTokenPercentage =
                            string.IsNullOrEmpty(tradePairMarketSnapshot.TotalSupply) ||
                            tradePairMarketSnapshot.TotalSupply == "0"
                                ? 0.0
                                : double.Parse(userLiquiditySnapshot.LpTokenAmount.ToDecimalsString(8)) / Double.Parse(tradePairMarketSnapshot.TotalSupply);
                        var lpTokenValueInUsd = lpTokenPercentage * tvl;
                        sumLpTokenInUsdBag.Add(lpTokenValueInUsd);
                        var token0FeeInUsd = Double.Parse(userLiquiditySnapshot.Token0TotalFee.ToDecimalsString(token0Decimal)) *
                                             token0Price;
                        var token1FeeInUsd = Double.Parse(userLiquiditySnapshot.Token1TotalFee.ToDecimalsString(token1Decimal)) *
                                             token1Price;
                        sumFeeBag.Add(token0FeeInUsd + token1FeeInUsd);
                    }
                }).ToArray();

                await Task.WhenAll(tasks);

                if (actualSnapshotCount == 0)
                {
                    return 0.0;
                }
                
                var sumLpTokenInUsd = sumLpTokenInUsdBag.Sum();
                var sumFeeInUsd = sumFeeBag.Sum();

                var avgLpTokenInUsd = sumLpTokenInUsd / actualSnapshotCount;

                _logger.LogInformation($"calculate EstimatedAPR input user address: {userLiquidityIndex.Address}, " +
                                       $"type: {type}, " +
                                       $"sumFee: {sumFeeInUsd}," +
                                       $"avgLpTokenInUsd: {avgLpTokenInUsd}, " +
                                       $"periodInDays: {periodInDays}");

                return avgLpTokenInUsd > 0 ? (sumFeeInUsd / avgLpTokenInUsd) * (360 / actualSnapshotCount) * 100 : 0;
            }
          
            case EstimatedAprType.All:
            {
                var unReveivedFee = Double.Parse(userLiquidityIndex.Token0UnReceivedFee.ToDecimalsString(token0Decimal)) *
                                    token0Price +
                                    Double.Parse(userLiquidityIndex.Token1UnReceivedFee.ToDecimalsString(token1Decimal)) *
                                    token1Price;
                var cumulativeAddition =
                    Double.Parse(userLiquidityIndex.Token0CumulativeAddition.ToDecimalsString(token0Decimal)) *
                    token0Price +
                    Double.Parse(userLiquidityIndex.Token1CumulativeAddition.ToDecimalsString(token1Decimal)) * token1Price;
                var averageHoldingPeriod = GetAverageHoldingPeriod(userLiquidityIndex);
                if (cumulativeAddition == 0 || averageHoldingPeriod == 0)
                {
                    return 0.0;
                }

                _logger.LogInformation($"calculate EstimatedAPR input user address: {userLiquidityIndex.Address}, " +
                                       $"type: {type}, " +
                                       $"unReveivedFee: {unReveivedFee}, " +
                                       $"cumulativeAddition: {cumulativeAddition}," +
                                       $"averageHoldingPeriod: {averageHoldingPeriod}");

                return ((unReveivedFee / cumulativeAddition) * (360d / averageHoldingPeriod)) * 100;
            }
            default:
            {
                return 0.0;
            }
        }
    }
    

    private async Task<Tuple<double, double, double>> CalculateAllEstimatedAPRAsync(
        int token0Decimal,
        int token1Decimal,
        double token0Price,
        double token1Price,
        CurrentUserLiquidityIndex userLiquidityIndex)
    {
        var week = await CalculateEstimatedAPRAsync(EstimatedAprType.Week, token0Decimal, token1Decimal, token0Price,
            token1Price, userLiquidityIndex);
        var month = await CalculateEstimatedAPRAsync(EstimatedAprType.Month, token0Decimal, token1Decimal, token0Price,
            token1Price, userLiquidityIndex);
        var all = await CalculateEstimatedAPRAsync(EstimatedAprType.All, token0Decimal, token1Decimal, token0Price,
            token1Price, userLiquidityIndex);
        return new Tuple<double, double, double>(week, month, all);
    }

    
    public async Task<List<TradePairPositionDto>> ProcessUserPositionAsync(GetUserPositionsDto input, List<CurrentUserLiquidityIndex> userLiquidityIndices)
    {
        var result = new List<TradePairPositionDto>();
        
        foreach (var userLiquidityIndex in userLiquidityIndices)
        {
            var tradePairGrain = _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(userLiquidityIndex.TradePairId));
            var pair = (await tradePairGrain.GetAsync()).Data;
            var currentTradePairGrain = _clusterClient.GetGrain<ICurrentTradePairGrain>(GrainIdHelper.GenerateGrainId(userLiquidityIndex.TradePairId));
            var currentTradePair = (await currentTradePairGrain.GetAsync()).Data;
            
            _logger.LogInformation($"process user position input address: {input.Address}, user liquidity index: {JsonConvert.SerializeObject(userLiquidityIndex)}");

            var lpTokenPercentage = currentTradePair.TotalSupply == 0
                ? 0.0
                : userLiquidityIndex.LpTokenAmount / (double)currentTradePair.TotalSupply;
            
            var token0Price = await _tokenPriceProvider.GetTokenUSDPriceAsync(pair.ChainId, pair.Token0.Symbol);
            var token1Price = await _tokenPriceProvider.GetTokenUSDPriceAsync(pair.ChainId, pair.Token1.Symbol);
            var token0ValueInUsd = lpTokenPercentage * pair.ValueLocked0 * token0Price;
            var token1ValueInUsd = lpTokenPercentage * pair.ValueLocked1 * token1Price;
            var token0Percenage = token0ValueInUsd / (token0ValueInUsd + token1ValueInUsd);
            var token1Percenage = token1ValueInUsd / (token0ValueInUsd + token1ValueInUsd);
            var token0PercentStr = Math.Round(token0Percenage * 100,2).ToString();
            var token1PercentStr = Math.Round(100 - double.Parse(token0PercentStr),2).ToString();
            var valueInUsd = lpTokenPercentage * pair.TVL;
            var token0UnReceivedFee =
                Double.Parse(userLiquidityIndex.Token0UnReceivedFee.ToDecimalsString(pair.Token0.Decimals));
            var token1UnReceivedFee =
                Double.Parse(userLiquidityIndex.Token1UnReceivedFee.ToDecimalsString(pair.Token1.Decimals));
            
            var cumulativeAdditionInUsd = Double.Parse(userLiquidityIndex.Token0CumulativeAddition.ToDecimalsString(pair.Token0.Decimals)) * token0Price +
                                          Double.Parse(userLiquidityIndex.Token1CumulativeAddition.ToDecimalsString(pair.Token1.Decimals)) * token1Price;
            var averageHoldingPeriod = GetAverageHoldingPeriod(userLiquidityIndex);
            
            var estimatedAPR = await CalculateAllEstimatedAPRAsync(
                pair.Token0.Decimals, 
                pair.Token1.Decimals,
                token0Price,
                token1Price,
                userLiquidityIndex);
            
            var dynamicAPR = (averageHoldingPeriod != 0 && cumulativeAdditionInUsd != 0)
                ? (valueInUsd - cumulativeAdditionInUsd) / cumulativeAdditionInUsd * (360d /
                  averageHoldingPeriod) * 100
                : 0;
            
            _logger.LogInformation($"process user position input address: {input.Address}, " +
                                   $"pair.Address: {pair.Address}, " +
                                   $"token0Price: {token0Price}, " +
                                   $"token1Price: {token1Price}, " +
                                   $"pair.TotalSupply: {pair.TotalSupply}, " +
                                   $"currentTradePair.TotalSupply: {currentTradePair.TotalSupply}, " +
                                   $"pair.ValueLocked0: {pair.ValueLocked0}, " +
                                   $"pair.ValueLocked1: {pair.ValueLocked1}, " +
                                   $"pair.TVL: {pair.TVL}, " +
                                   $"averageHoldingPeriod: {averageHoldingPeriod}," +
                                   $"lpTokenPercentage: {lpTokenPercentage}, " +
                                   $"valueInUsd: {valueInUsd}, " +
                                   $"dynamicAPR: {dynamicAPR}, " +
                                   $"estimatedAPR7d: {estimatedAPR.Item1}, " +
                                   $"estimatedAPR30d: {estimatedAPR.Item2}, " +
                                   $"estimatedAPRAll: {estimatedAPR.Item3}");

            result.Add(new TradePairPositionDto()
            {
                TradePairInfo = _objectMapper.Map<TradePairGrainDto, PositionTradePairDto>(pair),
                Token0Amount = (lpTokenPercentage * pair.ValueLocked0).ToString(),
                Token1Amount = (lpTokenPercentage * pair.ValueLocked1).ToString(),
                Token0Percent = token0PercentStr,
                Token1Percent = token1PercentStr,
                LpTokenAmount = userLiquidityIndex.LpTokenAmount.ToDecimalsString(8),
                Position = new LiquidityPoolValueInfo()
                {
                    ValueInUsd = valueInUsd.ToString(),
                    Token0Value = (lpTokenPercentage * pair.ValueLocked0).ToString(),
                    Token0ValueInUsd = (token0ValueInUsd).ToString(),
                    Token1Value = (lpTokenPercentage * pair.ValueLocked1).ToString(),
                    Token1ValueInUsd = (token1ValueInUsd).ToString(),
                },
                Fee = new LiquidityPoolValueInfo()
                {
                    ValueInUsd = (token0UnReceivedFee * token0Price + token1UnReceivedFee * token1Price).ToString(),
                    Token0Value = token0UnReceivedFee.ToString(),
                    Token0ValueInUsd = (token0UnReceivedFee * token0Price).ToString(),
                    Token1Value = token1UnReceivedFee.ToString(),
                    Token1ValueInUsd = (token1UnReceivedFee * token1Price).ToString(),
                },
                CumulativeAddition = new LiquidityPoolValueInfo()
                {
                    ValueInUsd = cumulativeAdditionInUsd.ToString(),
                    Token0Value = userLiquidityIndex.Token0CumulativeAddition.ToDecimalsString(pair.Token0.Decimals),
                    Token0ValueInUsd = (Double.Parse(userLiquidityIndex.Token0CumulativeAddition.ToDecimalsString(pair.Token0.Decimals)) * token0Price).ToString(),
                    Token1Value = userLiquidityIndex.Token1CumulativeAddition.ToDecimalsString(pair.Token1.Decimals),
                    Token1ValueInUsd = (Double.Parse(userLiquidityIndex.Token1CumulativeAddition.ToDecimalsString(pair.Token1.Decimals)) * token1Price).ToString(),
                },
                EstimatedAPR = new List<EstimatedAPR>()
                {
                    new EstimatedAPR()
                    {
                        Type = EstimatedAprType.Week,
                        Percent = estimatedAPR.Item1.ToString("F2")
                    },
                    new EstimatedAPR()
                    {
                        Type = EstimatedAprType.Month,
                        Percent = estimatedAPR.Item2.ToString("F2")
                    },
                    new EstimatedAPR()
                    {
                        Type = EstimatedAprType.All,
                        Percent = estimatedAPR.Item3.ToString("F2")
                    }
                },
                ImpermanentLossInUSD = (valueInUsd - cumulativeAdditionInUsd).ToString(),
                DynamicAPR = dynamicAPR.ToString()
            });
        }

        return result;
    }
    
    public async Task<PagedResultDto<TradePairPositionDto>> GetUserPositionsAsync(GetUserPositionsDto input)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        
        var mustQuery = new List<Func<QueryContainerDescriptor<CurrentUserLiquidityIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(input.Address)));
        QueryContainer Filter(QueryContainerDescriptor<CurrentUserLiquidityIndex> f) => f.Bool(b => b.Must(mustQuery));
        var list = await _currentUserLiquidityIndexRepository.GetSortListAsync(Filter,
            sortFunc: s => s.Descending(t => t.TradePairId),
            limit: input.MaxResultCount == 0 ? TradePairConst.MaxPageSize :
            input.MaxResultCount > TradePairConst.MaxPageSize ? TradePairConst.MaxPageSize : input.MaxResultCount,
            skip: input.SkipCount);
        var totalCount = await _currentUserLiquidityIndexRepository.CountAsync(Filter);
        var userPositions = await ProcessUserPositionAsync(input, list.Item2);
        
        stopwatch.Stop();
        var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        
        _logger.LogInformation($"GetUserPositionsAsync executed in {elapsedMilliseconds} ms, address: {input.Address}, trade pair count: {list.Item2.Count}");
        
        return new PagedResultDto<TradePairPositionDto>()
        {
            TotalCount = totalCount.Count,
            Items = userPositions
        };
    }
    
}