using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.MyPortfolio;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Tokens;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Etos;
using AwakenServer.Trade.Index;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Orleans;
using Serilog;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using ILogger = Serilog.ILogger;
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
    private readonly ILogger _logger;
    private readonly IOptionsSnapshot<PortfolioOptions> _portfolioOptions;

    public MyPortfolioAppService(IClusterClient clusterClient, 
        INESTRepository<TradePair, Guid> tradePairIndexRepository, 
        INESTRepository<CurrentUserLiquidityIndex, Guid> currentUserLiquidityIndexRepository,
        INESTRepository<UserLiquiditySnapshotIndex, Guid> userLiduiditySnapshotIndexRepository,
        INESTRepository<TradePairMarketDataSnapshot, Guid> tradePairSnapshotIndexRepository,
        IObjectMapper objectMapper,
        ITokenPriceProvider tokenPriceProvider,
        IDistributedCache<string> syncedTransactionIdCache,
        IDistributedEventBus distributedEventBus,
        IOptionsSnapshot<PortfolioOptions> portfolioOptions)
    {
        _clusterClient = clusterClient;
        _tradePairIndexRepository = tradePairIndexRepository;
        _currentUserLiquidityIndexRepository = currentUserLiquidityIndexRepository;
        _objectMapper = objectMapper;
        _userLiduiditySnapshotIndexRepository = userLiduiditySnapshotIndexRepository;
        _tradePairSnapshotIndexRepository = tradePairSnapshotIndexRepository;
        _tokenPriceProvider = tokenPriceProvider;
        _syncedTransactionIdCache = syncedTransactionIdCache;
        _distributedEventBus = distributedEventBus;
        _logger = Log.ForContext<MyPortfolioAppService>();
        _portfolioOptions = portfolioOptions;
    }
    
    private string AddVersionToKey(string baseKey, string version)
    {
        return $"{baseKey}:{version}";
    }

    [ExceptionHandler(typeof(Exception), Message = "UpdateUserAllAsset Error", ReturnDefault = ReturnDefault.Default)]
    public virtual async Task<int> UpdateUserAllAssetAsync(string address, TimeSpan maxTimeSinceLastUpdate, string dataVersion)
    {
        var affectedCount = 0;
        var userLiquidityIndexList = await GetCurrentUserLiquidityIndexListAsync(address, dataVersion);
        foreach (var userLiquidityIndex in userLiquidityIndexList)
        {
            var currentUserLiquidityGrain = _clusterClient.GetGrain<ICurrentUserLiquidityGrain>(AddVersionToKey(GrainIdHelper.GenerateGrainId(address, userLiquidityIndex.TradePairId), dataVersion));
            var currentUserLiquidityGrainResult = await currentUserLiquidityGrain.GetAsync();
            if (!currentUserLiquidityGrainResult.Success)
            {
                _logger.Error($"update user all liquidity address: {address}, can't user liquidity grain: {userLiquidityIndex.TradePairId}");
                continue;
            }

            var timeSinceLastUpdate = DateTime.UtcNow - currentUserLiquidityGrainResult.Data.LastUpdateTime;
            if (timeSinceLastUpdate <= maxTimeSinceLastUpdate)
            {
                continue;
            }
            
            var tradePairGrain =
                _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(userLiquidityIndex.TradePairId));
            var pairResultDto = await tradePairGrain.GetAsync();
            if (!pairResultDto.Success)
            {
                _logger.Error($"update user all liquidity address: {address}, can't find pair: {userLiquidityIndex.TradePairId}");
                continue;
            }

            var pair = pairResultDto.Data;
            var currentTradePairGrain = _clusterClient.GetGrain<ICurrentTradePairGrain>(
                AddVersionToKey(GrainIdHelper.GenerateGrainId(userLiquidityIndex.TradePairId),
                    dataVersion));
            var currentTradePairResultDto = await currentTradePairGrain.GetAsync();
            if (!currentTradePairResultDto.Success)
            {
                _logger.Error($"update user all liquidity address: {address}, can't find current pair: {userLiquidityIndex.TradePairId}");
                continue;
            }

            var currentTradePair = currentTradePairResultDto.Data;
            var lpTokenPercentage = currentTradePair.TotalSupply == 0
                ? 0.0
                : currentUserLiquidityGrainResult.Data.LpTokenAmount / (double)currentTradePair.TotalSupply;
            
            currentUserLiquidityGrainResult.Data.Version = dataVersion;
            currentUserLiquidityGrainResult.Data.AssetInUSD = lpTokenPercentage * pair.TVL;
            await _distributedEventBus.PublishAsync(
                ObjectMapper.Map<CurrentUserLiquidityGrainDto, CurrentUserLiquidityEto>(currentUserLiquidityGrainResult.Data));
            _logger.Information(
                $"update user all liquidity address: {address}, pair id:{pair.Id}, pair address: {pair.Address}, index: {JsonConvert.SerializeObject(currentUserLiquidityGrainResult.Data)}");
            ++affectedCount;
        }

        return affectedCount;
    }
    
    [ExceptionHandler(typeof(Exception), Message = "SyncLiquidityRecord Error", TargetType = typeof(HandlerExceptionService), MethodName = nameof(HandlerExceptionService.HandleWithReturn))]
    public virtual async Task<bool> SyncLiquidityRecordAsync(LiquidityRecordDto liquidityRecordDto, string dataVersion, bool alignUserAllAsset)
    {
        var key = AddVersionToKey($"{SyncedTransactionCachePrefix}:{liquidityRecordDto.TransactionHash}", dataVersion);
        var existed = await _syncedTransactionIdCache.GetAsync(key);
        if (!existed.IsNullOrWhiteSpace())
        {
            return false;
        }
        var tradePair = await GetTradePairAsync(liquidityRecordDto.ChainId, liquidityRecordDto.Pair, dataVersion);
        if (tradePair == null)
        {
            _logger.Information("can not find trade pair: {chainId}, {pairAddress}", liquidityRecordDto.ChainId,
                liquidityRecordDto.Pair);
            return false;
        }
        var tradePairGrain = _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(tradePair.Id));
        var tradePairGrainResultDto = await tradePairGrain.GetAsync();
        if (!tradePairGrainResultDto.Success)
        {
            _logger.Information("can not find trade pair grain: {chainId}, {pairId}", liquidityRecordDto.ChainId,
                tradePair.Id);
            return false;
        }
        var currentTradePairGrain = _clusterClient.GetGrain<ICurrentTradePairGrain>(AddVersionToKey(GrainIdHelper.GenerateGrainId(tradePair.Id), dataVersion));
        var currentTradePairGrainResultDto = await currentTradePairGrain.AddTotalSupplyAsync(tradePair.Id, liquidityRecordDto.Type == LiquidityType.Mint ? 
            liquidityRecordDto.LpTokenAmount : -liquidityRecordDto.LpTokenAmount, liquidityRecordDto.Timestamp);
        
        var currentUserLiquidityGrain = _clusterClient.GetGrain<ICurrentUserLiquidityGrain>(AddVersionToKey(GrainIdHelper.GenerateGrainId(liquidityRecordDto.Address, tradePair.Id), dataVersion));
        var currentUserLiquidityGrainResult = liquidityRecordDto.Type == LiquidityType.Mint
            ? await currentUserLiquidityGrain.AddLiquidityAsync(liquidityRecordDto, tradePair.Id, tradePair.Token0.Symbol)
            : await currentUserLiquidityGrain.RemoveLiquidityAsync(liquidityRecordDto, tradePair.Id, tradePair.Token0.Symbol);

        var lpTokenPercentage = currentTradePairGrainResultDto.Data.TotalSupply == 0
            ? 0.0
            : currentUserLiquidityGrainResult.Data.LpTokenAmount / (double)currentTradePairGrainResultDto.Data.TotalSupply;
        
        // publish eto
        currentUserLiquidityGrainResult.Data.Version = dataVersion;
        currentUserLiquidityGrainResult.Data.AssetInUSD = lpTokenPercentage * tradePairGrainResultDto.Data.TVL;
        await _distributedEventBus.PublishAsync(
            ObjectMapper.Map<CurrentUserLiquidityGrainDto, CurrentUserLiquidityEto>(currentUserLiquidityGrainResult.Data));
        _logger.Information(
            $"update user liquidity address: {liquidityRecordDto.Address}, pair id:{tradePair.Id}, pair address: {tradePair.Address}, {currentUserLiquidityGrainResult.Data.LpTokenAmount}, {currentTradePairGrainResultDto.Data.TotalSupply}, {lpTokenPercentage}, {tradePair.TVL}, index: {JsonConvert.SerializeObject(currentUserLiquidityGrainResult.Data)}");
        if (alignUserAllAsset)
        {
            await UpdateUserAllAssetAsync(liquidityRecordDto.Address, TimeSpan.FromMilliseconds(0), dataVersion);
        }
        var userLiquiditySnapshotGrainDto = new UserLiquiditySnapshotGrainDto()
        {
            Address = liquidityRecordDto.Address,
            TradePairId = tradePair.Id,
            LpTokenAmount = currentUserLiquidityGrainResult.Data.LpTokenAmount,
            SnapShotTime = currentUserLiquidityGrainResult.Data.LastUpdateTime.Date,
            Version = dataVersion
        };
        var userLiquiditySnapshotGrain = _clusterClient.GetGrain<IUserLiquiditySnapshotGrain>(
            AddVersionToKey(GrainIdHelper.GenerateGrainId(liquidityRecordDto.Address, tradePair.Id, userLiquiditySnapshotGrainDto.SnapShotTime), dataVersion));
        var userLiquiditySnapshotResult = await userLiquiditySnapshotGrain.AddOrUpdateAsync(userLiquiditySnapshotGrainDto);
        // publish eto
        userLiquiditySnapshotResult.Data.Version = dataVersion;
        await _distributedEventBus.PublishAsync(ObjectMapper.Map<UserLiquiditySnapshotGrainDto, UserLiquiditySnapshotEto>(userLiquiditySnapshotResult.Data));
        await _syncedTransactionIdCache.SetAsync(key, "1", new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddYears(1)
        });
        return true;
    }
    
    [ExceptionHandler(typeof(Exception), Message = "SyncSwapRecord Error", TargetType = typeof(HandlerExceptionService), MethodName = nameof(HandlerExceptionService.HandleWithReturn))]
    public virtual async Task<bool> SyncSwapRecordAsync(SwapRecordDto swapRecordDto, string dataVersion)
    {
        var key = AddVersionToKey($"{SyncedTransactionCachePrefix}:{swapRecordDto.TransactionHash}", dataVersion);
        var existed = await _syncedTransactionIdCache.GetAsync(key);
        if (!existed.IsNullOrWhiteSpace())
        {
            return false;
        }
        await SyncSingleSwapRecordAsync(swapRecordDto, dataVersion);
        if (!swapRecordDto.SwapRecords.IsNullOrEmpty())
        {
            foreach (var swapRecord in swapRecordDto.SwapRecords)
            {
                ObjectMapper.Map(swapRecord, swapRecordDto);
                await SyncSingleSwapRecordAsync(swapRecordDto, dataVersion);
            }
        }
        await _syncedTransactionIdCache.SetAsync(key, "1");
        return true;
    }

    public async Task<bool> SyncSingleSwapRecordAsync(SwapRecordDto swapRecordDto, string dataVersion)
    {
        var tradePair = await GetTradePairAsync(swapRecordDto.ChainId, swapRecordDto.PairAddress, dataVersion);
        if (tradePair == null)
        {
            _logger.Information("can not find trade pair: {chainId}, {pairAddress}", swapRecordDto.ChainId,
                swapRecordDto.PairAddress);
            return false;
        }
        var tradePairGrain = _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(tradePair.Id));
        var tradePairGrainResultDto = await tradePairGrain.GetAsync();
        if (!tradePairGrainResultDto.Success)
        {
            _logger.Information("can not find trade pair grain: {chainId}, {pairId}", swapRecordDto.ChainId,
                tradePair.Id);
            return false;
        }
        var currentTradePairGrain = _clusterClient.GetGrain<ICurrentTradePairGrain>(AddVersionToKey(GrainIdHelper.GenerateGrainId(tradePair.Id), dataVersion));
        var isToken0 = swapRecordDto.SymbolIn == tradePair.Token0.Symbol;
        var total0Fee = isToken0 ? swapRecordDto.TotalFee : 0;
        var total1Fee = isToken0 ? 0 : swapRecordDto.TotalFee;
        var currentTradePairResult = await currentTradePairGrain.AddTotalFeeAsync(tradePair.Id, total0Fee, total1Fee);

        var userLiquidityList = await GetCurrentUserLiquidityIndexListAsync(tradePair.Id, dataVersion);
        foreach (var userLiquidity in userLiquidityList)
        {
            var percent = (double)userLiquidity.LpTokenAmount / currentTradePairResult.Data.TotalSupply;
            var userToken0Fee = (long)(total0Fee * percent);
            var userToken1Fee = (long)(total1Fee * percent);
            if (userToken0Fee == 0 && userToken1Fee == 0)
            {
                continue;
            }
            var currentLiquidityGrain = _clusterClient.GetGrain<ICurrentUserLiquidityGrain>(AddVersionToKey(GrainIdHelper.GenerateGrainId(userLiquidity.Address, tradePair.Id), dataVersion));
            var currentLiquidityGrainResult = await currentLiquidityGrain.AddTotalFee(userToken0Fee, userToken1Fee, swapRecordDto);
            var lpTokenPercentage = currentTradePairResult.Data.TotalSupply == 0
                ? 0.0
                : currentLiquidityGrainResult.Data.LpTokenAmount / (double)currentTradePairResult.Data.TotalSupply;
            // publish CurrentUserLiquidityEto
            currentLiquidityGrainResult.Data.Version = dataVersion;
            currentLiquidityGrainResult.Data.AssetInUSD = lpTokenPercentage * tradePairGrainResultDto.Data.TVL;
            await _distributedEventBus.PublishAsync(
                ObjectMapper.Map<CurrentUserLiquidityGrainDto, CurrentUserLiquidityEto>(currentLiquidityGrainResult.Data));
            
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
                AddVersionToKey(GrainIdHelper.GenerateGrainId(userLiquidity.Address, tradePair.Id, currentLiquidityGrainResult.Data.LastUpdateTime.Date), dataVersion));
            var snapshotResult = await snapshotGrain.AddOrUpdateAsync(userLiquiditySnapshotGrainDto);
            // publish UserLiquiditySnapshotEto
            snapshotResult.Data.Version = dataVersion;
            await _distributedEventBus.PublishAsync(ObjectMapper.Map<UserLiquiditySnapshotGrainDto, UserLiquiditySnapshotEto>(snapshotResult.Data));
        }
        return true;
    }


    public async Task<TradePair> GetTradePairAsync(string chainName, string address, string dataVersion)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TradePair>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainName)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(address)));

        QueryContainer Filter(QueryContainerDescriptor<TradePair> f) => f.Bool(b => b.Must(mustQuery));
        return await _tradePairIndexRepository.GetAsync(Filter);
    }

    public async Task<List<CurrentUserLiquidityIndex>> GetCurrentUserLiquidityIndexListAsync(Guid tradePairId, string dataVersion)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CurrentUserLiquidityIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.TradePairId).Value(tradePairId)));
        mustQuery.Add(q => q.Range(i => i.Field(f => f.LpTokenAmount).GreaterThan(0)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Version).Value(dataVersion)));
        QueryContainer Filter(QueryContainerDescriptor<CurrentUserLiquidityIndex> f) => f.Bool(b => b.Must(mustQuery));
        var result = await _currentUserLiquidityIndexRepository.GetListAsync(Filter, skip: 0, limit: 10000);
        return result.Item2;
    }
    
    public async Task<List<CurrentUserLiquidityIndex>> GetCurrentUserLiquidityIndexListAsync(string address, string dataVersion)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CurrentUserLiquidityIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(address)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Version).Value(dataVersion)));
        mustQuery.Add(q => q.Range(i => i.Field(f => f.LpTokenAmount).GreaterThan(0)));

        QueryContainer Filter(QueryContainerDescriptor<CurrentUserLiquidityIndex> f) => f.Bool(b => b.Must(mustQuery));
        var result = await _currentUserLiquidityIndexRepository.GetListAsync(Filter, skip: 0, limit: 10000);
        return result.Item2;
    }
    
    public async Task<CurrentUserLiquidityIndex> GetCurrentUserLiquidityIndexAsync(Guid tradePairId, string address, string dataVersion)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CurrentUserLiquidityIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.TradePairId).Value(tradePairId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(address)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Version).Value(dataVersion)));

        QueryContainer Filter(QueryContainerDescriptor<CurrentUserLiquidityIndex> f) => f.Bool(b => b.Must(mustQuery));
        var result = await _currentUserLiquidityIndexRepository.GetListAsync(Filter, skip: 0, limit: 1);
        return result.Item2.IsNullOrEmpty() ? new CurrentUserLiquidityIndex() : result.Item2[0];
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
        
        return result;
    }
    
    public List<TokenPortfolioInfoDto> MergeAndProcess(Dictionary<string, TokenPortfolioInfoDto> rawList, int showCount, double total)
    {
        showCount = showCount >= 1 ? showCount - 1 : 0;
        var result = new List<TokenPortfolioInfoDto>();
        
        var sortedPositionDistributions = rawList
            .Where(u => double.TryParse(u.Value.ValueInUsd, out _))
            .OrderByDescending(u => double.Parse(u.Value.ValueInUsd))
            .ToList();
        
        _logger.Information($"sortedPositionDistributions: {JsonConvert.SerializeObject(sortedPositionDistributions)}");
        
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
            else if(result.Count > 0)
            {
                var sumValueInUsd = double.Parse(result[result.Count - 1].ValueInUsd) + double.Parse(tokenInfoPair.Value.ValueInUsd);
                var sumPercent = double.Parse(result[result.Count - 1].ValuePercent) + double.Parse(tokenInfoPair.Value.ValuePercent);
                result[result.Count - 1].ValueInUsd = sumValueInUsd.ToString();
                result[result.Count - 1].ValuePercent = sumPercent.ToString("F2");
            }
        }
        
        // Ensure the sum of all ValuePercent is exactly 100%
        double finalTotalPercent = result.Sum(r => double.Parse(r.ValuePercent));
        if (finalTotalPercent != 100.00 && result.Count > 0)
        {
            double difference = 100.00 - finalTotalPercent;
            result[result.Count - 1].ValuePercent = (double.Parse(result[result.Count - 1].ValuePercent) + difference).ToString("F2");
        }
        
        return result;
    }
    
    public async Task<UserPortfolioDto> GetUserPortfolioAsync(GetUserPortfolioDto input)
    {
        var dataVersion = _portfolioOptions.Value.DataVersion;        
        var positionDistributions = new List<TradePairPortfolioDto>();
        var feeDistributions = new List<TradePairPortfolioDto>();
        var list = await GetCurrentUserLiquidityIndexListAsync(input.Address, dataVersion);
        
        var sumValueInUsd = 0.0;
        var sumFeeInUsd = 0.0;
        var tokenPositionDictionary = new Dictionary<string, TokenPortfolioInfoDto>();
        var tokenFeeDictionary = new Dictionary<string, TokenPortfolioInfoDto>();
        
        foreach (var userLiquidityIndex in list)
        {
            var tradePairGrain = _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(userLiquidityIndex.TradePairId));
            var pair = (await tradePairGrain.GetAsync()).Data;
            var currentTradePairGrain = _clusterClient.GetGrain<ICurrentTradePairGrain>(AddVersionToKey(GrainIdHelper.GenerateGrainId(userLiquidityIndex.TradePairId), dataVersion));
            var currentTradePair = (await currentTradePairGrain.GetAsync()).Data;
            _logger.Information($"CurrentTradePair:{JsonConvert.SerializeObject(currentTradePair)}");
            var lpTokenPercentage = currentTradePair.TotalSupply == 0
                ? 0.0
                : userLiquidityIndex.LpTokenAmount / (double)currentTradePair.TotalSupply;
            
            var token0Price = await _tokenPriceProvider.GetTokenUSDPriceAsync(pair.ChainId, pair.Token0.Symbol);
            var token1Price = await _tokenPriceProvider.GetTokenUSDPriceAsync(pair.ChainId, pair.Token1.Symbol);
            var token0ValueInUsd = lpTokenPercentage * pair.ValueLocked0 * token0Price;
            var token1ValueInUsd = lpTokenPercentage * pair.ValueLocked1 * token1Price;
            var valueInUsd = lpTokenPercentage * (pair.ValueLocked0 * token0Price + pair.ValueLocked1 * token1Price);
            var token0CumulativeFee = token0Price * Double.Parse((userLiquidityIndex.Token0UnReceivedFee + userLiquidityIndex.Token0ReceivedFee).ToDecimalsString(pair.Token0.Decimals));
            var token1CumulativeFee = token1Price * Double.Parse((userLiquidityIndex.Token1UnReceivedFee + userLiquidityIndex.Token1ReceivedFee).ToDecimalsString(pair.Token1.Decimals));

            var fee = token0CumulativeFee + token1CumulativeFee;
            
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
            
            var currentToken0Fee = double.Parse(tokenFeeDictionary[pair.Token0.Symbol].ValueInUsd) + token0CumulativeFee;
            tokenFeeDictionary[pair.Token0.Symbol].ValueInUsd = currentToken0Fee.ToString();
            
            var currentToken1Fee = double.Parse(tokenFeeDictionary[pair.Token1.Symbol].ValueInUsd) + token1CumulativeFee;
            tokenFeeDictionary[pair.Token1.Symbol].ValueInUsd = currentToken1Fee.ToString();
        }
        
        _logger.Information($"GetUserPortfolioAsync, address: {input.Address}");
        
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
        long periodInDays, string dataVersion)
    {
        var mustQuery =
            new List<Func<QueryContainerDescriptor<UserLiquiditySnapshotIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.TradePairId).Value(tradePairId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(userAddress)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Version).Value(dataVersion)));
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
            chainId, tradePairId, timestampMin, dataVersion);
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
        Guid tradePairId, DateTime timestampMax, string dataVersion)
    {
        var mustQuery =
            new List<Func<QueryContainerDescriptor<UserLiquiditySnapshotIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.TradePairId).Value(tradePairId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(userAddress)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Version).Value(dataVersion)));
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
        CurrentUserLiquidityIndex userLiquidityIndex,
        string dataVersion)
    {
        switch (type)
        {
            case EstimatedAprType.Week:
            case EstimatedAprType.Month:
            {
                var periodInDays = GetDayDifference(type, userLiquidityIndex);
                var userLiquiditySnapshots = await GetSnapshotIndexListAsync(userLiquidityIndex.Address,
                    userLiquidityIndex.ChainId, userLiquidityIndex.TradePairId,
                    periodInDays,
                    dataVersion);

                _logger.Information($"calculate EstimatedAPR input user address: {userLiquidityIndex.Address}, " +
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

                _logger.Information($"calculate EstimatedAPR input user address: {userLiquidityIndex.Address}, " +
                                       $"type: {type}, " +
                                       $"sumFee: {sumFeeInUsd}," +
                                       $"avgLpTokenInUsd: {avgLpTokenInUsd}, " +
                                       $"periodInDays: {periodInDays}");

                return avgLpTokenInUsd > 0 ? (sumFeeInUsd / avgLpTokenInUsd) * (360 / actualSnapshotCount) * 100 : 0;
            }
          
            case EstimatedAprType.All:
            {
                var fee = Double.Parse((userLiquidityIndex.Token0UnReceivedFee + userLiquidityIndex.Token0ReceivedFee).ToDecimalsString(token0Decimal)) *
                                    token0Price +
                                    Double.Parse((userLiquidityIndex.Token1UnReceivedFee + userLiquidityIndex.Token1ReceivedFee).ToDecimalsString(token1Decimal)) *
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

                _logger.Information($"calculate EstimatedAPR input user address: {userLiquidityIndex.Address}, " +
                                       $"type: {type}, " +
                                       $"unReveivedFee: {fee}, " +
                                       $"cumulativeAddition: {cumulativeAddition}," +
                                       $"averageHoldingPeriod: {averageHoldingPeriod}");

                return ((fee / cumulativeAddition) * (360d / averageHoldingPeriod)) * 100;
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
        CurrentUserLiquidityIndex userLiquidityIndex,
        string dataVersion)
    {
        var week = await CalculateEstimatedAPRAsync(EstimatedAprType.Week, token0Decimal, token1Decimal, token0Price,
            token1Price, userLiquidityIndex, dataVersion);
        var month = await CalculateEstimatedAPRAsync(EstimatedAprType.Month, token0Decimal, token1Decimal, token0Price,
            token1Price, userLiquidityIndex, dataVersion);
        var all = await CalculateEstimatedAPRAsync(EstimatedAprType.All, token0Decimal, token1Decimal, token0Price,
            token1Price, userLiquidityIndex, dataVersion);
        return new Tuple<double, double, double>(week, month, all);
    }

    
    public async Task<List<TradePairPositionDto>> ProcessUserPositionAsync(GetUserPositionsDto input, List<CurrentUserLiquidityIndex> userLiquidityIndices, string dataVersion)
    {
        var result = new List<TradePairPositionDto>();
        
        foreach (var userLiquidityIndex in userLiquidityIndices)
        {
            var tradePairGrain = _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(userLiquidityIndex.TradePairId));
            var pair = (await tradePairGrain.GetAsync()).Data;
            var currentTradePairGrain = _clusterClient.GetGrain<ICurrentTradePairGrain>(AddVersionToKey(GrainIdHelper.GenerateGrainId(userLiquidityIndex.TradePairId), dataVersion));
            var currentTradePair = (await currentTradePairGrain.GetAsync()).Data;
            
            _logger.Information($"process user position input address: {input.Address}, user liquidity index: {JsonConvert.SerializeObject(userLiquidityIndex)}");

            var lpTokenPercentage = currentTradePair.TotalSupply == 0
                ? 0.0
                : userLiquidityIndex.LpTokenAmount / (double)currentTradePair.TotalSupply;
            
            var (token0Price, token1Price) = await _tokenPriceProvider.GetUSDPriceAsync(pair.ChainId, pair.Id, pair.Token0.Symbol, pair.Token1.Symbol);
            
            var token0ValueInUsd = lpTokenPercentage * pair.ValueLocked0 * token0Price;
            var token1ValueInUsd = lpTokenPercentage * pair.ValueLocked1 * token1Price;
            var token0Percenage = token0ValueInUsd + token1ValueInUsd == 0 ? 0 : token0ValueInUsd / (token0ValueInUsd + token1ValueInUsd);
            var token0PercentStr = Math.Round(token0Percenage * 100,2).ToString();
            var token1PercentStr = token0ValueInUsd + token1ValueInUsd == 0 ? "0" : Math.Round(100 - double.Parse(token0PercentStr),2).ToString();
            
            var valueInUsd = lpTokenPercentage * (pair.ValueLocked0 * token0Price + pair.ValueLocked1 * token1Price);
            var token0CumulativeFeeAmount =
                Double.Parse((userLiquidityIndex.Token0UnReceivedFee + userLiquidityIndex.Token0ReceivedFee).ToDecimalsString(pair.Token0.Decimals));
            var token1CumulativeFeeAmount =
                Double.Parse((userLiquidityIndex.Token1UnReceivedFee + userLiquidityIndex.Token1ReceivedFee).ToDecimalsString(pair.Token1.Decimals));
            
            var token0CumulativeFeeInUsd = token0CumulativeFeeAmount * token0Price;
            var token1CumulativeFeeInUsd = token1CumulativeFeeAmount * token1Price;
            var token0FeePercent = token0CumulativeFeeInUsd + token1CumulativeFeeInUsd == 0 ? 0 : token0CumulativeFeeInUsd / (token0CumulativeFeeInUsd + token1CumulativeFeeInUsd);
            var token0FeePercentStr = Math.Round(token0FeePercent * 100,2).ToString();
            var token1FeePercentStr = token0CumulativeFeeInUsd + token1CumulativeFeeInUsd == 0 ? "0" : Math.Round(100 - double.Parse(token0FeePercentStr),2).ToString();

            var token0CumulativeAdditionAmountInUsd =
                Double.Parse(userLiquidityIndex.Token0CumulativeAddition.ToDecimalsString(pair.Token0.Decimals)) *
                token0Price;
            var token1CumulativeAdditionAmountInUsd =
                Double.Parse(userLiquidityIndex.Token1CumulativeAddition.ToDecimalsString(pair.Token1.Decimals)) *
                token1Price;
            var cumulativeAdditionInUsd = token0CumulativeAdditionAmountInUsd + token1CumulativeAdditionAmountInUsd;
            var token0CumulativeAdditionPercent = token0CumulativeAdditionAmountInUsd + token1CumulativeAdditionAmountInUsd == 0 ? 0 : token0CumulativeAdditionAmountInUsd / (token0CumulativeAdditionAmountInUsd + token1CumulativeAdditionAmountInUsd);
            var token0CumulativeAdditionPercentStr = Math.Round(token0CumulativeAdditionPercent * 100,2).ToString();
            var token1CumulativeAdditionPercentStr = token0CumulativeAdditionAmountInUsd + token1CumulativeAdditionAmountInUsd == 0 ? "0" : Math.Round(100 - double.Parse(token0CumulativeAdditionPercentStr),2).ToString();
            
            var averageHoldingPeriod = GetAverageHoldingPeriod(userLiquidityIndex);
            
            var estimatedAPR = await CalculateAllEstimatedAPRAsync(
                pair.Token0.Decimals, 
                pair.Token1.Decimals,
                token0Price,
                token1Price,
                userLiquidityIndex,
                dataVersion);
            
            var dynamicAPR = (averageHoldingPeriod != 0 && cumulativeAdditionInUsd != 0)
                ? (valueInUsd - cumulativeAdditionInUsd) / cumulativeAdditionInUsd * (360d /
                    averageHoldingPeriod) * 100
                : 0;
            
            _logger.Information($"process user position input address: {input.Address}, " +
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

            var positionTradePairDto = _objectMapper.Map<TradePairGrainDto, PositionTradePairDto>(pair);
            positionTradePairDto.Volume24hInUsd = (double.Parse(positionTradePairDto.Volume24h) * token0Price).ToString();
            
            result.Add(new TradePairPositionDto()
            {
                TradePairInfo = positionTradePairDto,
                LpTokenAmount = userLiquidityIndex.LpTokenAmount.ToDecimalsString(8),
                LpTokenPercent = (lpTokenPercentage * 100).ToString(),
                Position = new LiquidityPoolValueInfo()
                {
                    ValueInUsd = valueInUsd.ToString(),
                    Token0Amount = (lpTokenPercentage * pair.ValueLocked0).ToString(),
                    Token0AmountInUsd = (token0ValueInUsd).ToString(),
                    Token1Amount = (lpTokenPercentage * pair.ValueLocked1).ToString(),
                    Token1AmountInUsd = (token1ValueInUsd).ToString(),
                    Token0Percent = token0PercentStr,
                    Token1Percent = token1PercentStr,
                },
                Fee = new LiquidityPoolValueInfo()
                {
                    ValueInUsd = (token0CumulativeFeeInUsd + token1CumulativeFeeInUsd).ToString(),
                    Token0Amount = token0CumulativeFeeAmount.ToString(),
                    Token0AmountInUsd = (token0CumulativeFeeInUsd).ToString(),
                    Token1Amount = token1CumulativeFeeAmount.ToString(),
                    Token1AmountInUsd = (token1CumulativeFeeInUsd).ToString(),
                    Token0Percent = token0FeePercentStr,
                    Token1Percent = token1FeePercentStr,
                },
                CumulativeAddition = new LiquidityPoolValueInfo()
                {
                    ValueInUsd = cumulativeAdditionInUsd.ToString(),
                    Token0Amount = userLiquidityIndex.Token0CumulativeAddition.ToDecimalsString(pair.Token0.Decimals),
                    Token0AmountInUsd = (token0CumulativeAdditionAmountInUsd).ToString(),
                    Token1Amount = userLiquidityIndex.Token1CumulativeAddition.ToDecimalsString(pair.Token1.Decimals),
                    Token1AmountInUsd = (token1CumulativeAdditionAmountInUsd).ToString(),
                    Token0Percent = token0CumulativeAdditionPercentStr,
                    Token1Percent = token1CumulativeAdditionPercentStr,
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
                ImpermanentLossInUSD = (valueInUsd - cumulativeAdditionInUsd - (token0CumulativeFeeInUsd + token1CumulativeFeeInUsd)).ToString(),
                DynamicAPR = dynamicAPR.ToString()
            });
        }

        return result;
    }
    
    public async Task<PagedResultDto<TradePairPositionDto>> GetUserPositionsAsync(GetUserPositionsDto input)
    {
        var dataVersion = _portfolioOptions.Value.DataVersion;
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        
        var mustQuery = new List<Func<QueryContainerDescriptor<CurrentUserLiquidityIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(input.Address)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Version).Value(dataVersion)));
        mustQuery.Add(q => q.Range(i => i.Field(f => f.LpTokenAmount).GreaterThan(0)));

        QueryContainer Filter(QueryContainerDescriptor<CurrentUserLiquidityIndex> f) => f.Bool(b => b.Must(mustQuery));
        var list = await _currentUserLiquidityIndexRepository.GetSortListAsync(Filter,
            sortFunc: s => s.Descending(t => t.AssetInUSD),
            limit: input.MaxResultCount == 0 ? TradePairConst.MaxPageSize :
            input.MaxResultCount > TradePairConst.MaxPageSize ? TradePairConst.MaxPageSize : input.MaxResultCount,
            skip: input.SkipCount);
        var totalCount = await _currentUserLiquidityIndexRepository.CountAsync(Filter);
        var userPositions = await ProcessUserPositionAsync(input, list.Item2, dataVersion);
        
        stopwatch.Stop();
        var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        
        _logger.Information($"GetUserPositionsAsync executed in {elapsedMilliseconds} ms, address: {input.Address}, trade pair count: {list.Item2.Count}");
        
        return new PagedResultDto<TradePairPositionDto>()
        {
            TotalCount = totalCount.Count,
            Items = userPositions
        };
    }
    
    public async Task<List<string>> GetAllUserAddressesAsync(string dataVersion)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CurrentUserLiquidityIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Version).Value(dataVersion)));
        QueryContainer Filter(QueryContainerDescriptor<CurrentUserLiquidityIndex> f) => f.Bool(b => b.Must(mustQuery));
        int pageSize = 10000; 
        int currentPage = 0;
        bool hasMoreData = true;
        HashSet<string> uniqueAddresses = new HashSet<string>();

        while (hasMoreData)
        {
            var pagedData = await _currentUserLiquidityIndexRepository.GetSortListAsync(Filter, 
                sortFunc: s => s.Descending(t => t.Address),
                skip: currentPage * pageSize, limit: pageSize);
            if (pagedData.Item2.Count > 0)
            {
                foreach (var item in pagedData.Item2)
                {
                    uniqueAddresses.Add(item.Address); 
                }

                currentPage++;
            }
            else
            {
                hasMoreData = false;
            }
        }

        return uniqueAddresses.ToList();
    }

    public async Task<bool> CleanupUserLiquidityDataAsync(string dataVersion, bool executeDeletion)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CurrentUserLiquidityIndex>, QueryContainer>>();
        if (string.IsNullOrEmpty(dataVersion))
        {
            mustQuery.Add(q => !q.Exists(e => e.Field(f => f.Version)));
        }
        else
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Version).Value(dataVersion)));
        }
        QueryContainer Filter(QueryContainerDescriptor<CurrentUserLiquidityIndex> f) => f.Bool(b => b.Must(mustQuery));
        var affectedCount = 0;
        int pageSize = 10000; 
        int currentPage = 0;
        bool hasMoreData = true;
        while (hasMoreData)
        {
            var pagedData = await _currentUserLiquidityIndexRepository.GetSortListAsync(Filter, 
                sortFunc: s => s.Descending(t => t.Address),
                skip: currentPage * pageSize, limit: pageSize);
            if (pagedData.Item2.Count > 0)
            {
                var first10Items = pagedData.Item2.Take(5).ToList();
                foreach (var item in first10Items)
                {
                    _logger.Information($"Data cleanup, index: {typeof(CurrentUserLiquidityIndex).Name.ToLower()}, version: {dataVersion}, will remove data: {JsonConvert.SerializeObject(item)}");
                }
                if (executeDeletion)
                {
                    await _currentUserLiquidityIndexRepository.BulkDeleteAsync(pagedData.Item2);
                    _logger.Information($"Data cleanup, execute deletion index: {typeof(CurrentUserLiquidityIndex).Name.ToLower()}, version: {dataVersion}, page count: {pagedData.Item2.Count}");
                }
                currentPage++;
                affectedCount += pagedData.Item2.Count;
            }
            else
            {
                hasMoreData = false;
            }
        }
        _logger.Information($"Data cleanup, index: {typeof(CurrentUserLiquidityIndex).Name.ToLower()}, version: {dataVersion}, filter data count: {affectedCount}");
        return true;
    }

    public async Task<bool> CleanupUserLiquiditySnapshotsDataAsync(string dataVersion, bool executeDeletion)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserLiquiditySnapshotIndex>, QueryContainer>>();
        if (string.IsNullOrEmpty(dataVersion))
        {
            mustQuery.Add(q => !q.Exists(e => e.Field(f => f.Version)));
        }
        else
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Version).Value(dataVersion)));
        }
        QueryContainer Filter(QueryContainerDescriptor<UserLiquiditySnapshotIndex> f) => f.Bool(b => b.Must(mustQuery));
        var affectedCount = 0;
        int pageSize = 10000; 
        int currentPage = 0;
        bool hasMoreData = true;
        while (hasMoreData)
        {
            var pagedData = await _userLiduiditySnapshotIndexRepository.GetSortListAsync(Filter, 
                sortFunc: s => s.Descending(t => t.Address),
                skip: currentPage * pageSize, limit: pageSize);
            if (pagedData.Item2.Count > 0)
            {
                var first10Items = pagedData.Item2.Take(5).ToList();
                foreach (var item in first10Items)
                {
                    _logger.Information($"Data cleanup, index: {typeof(UserLiquiditySnapshotIndex).Name.ToLower()}, version: {dataVersion}, will remove data: {JsonConvert.SerializeObject(item)}");
                }
                if (executeDeletion)
                {
                    await _userLiduiditySnapshotIndexRepository.BulkDeleteAsync(pagedData.Item2);
                    _logger.Information($"Data cleanup, execute deletion index: {typeof(UserLiquiditySnapshotIndex).Name.ToLower()}, version: {dataVersion}, page count: {pagedData.Item2.Count}");
                }
                currentPage++;
                affectedCount += pagedData.Item2.Count;
            }
            else
            {
                hasMoreData = false;
            }
        }
        _logger.Information($"Data cleanup, index: {typeof(UserLiquiditySnapshotIndex).Name.ToLower()}, version: {dataVersion}, filter data count: {affectedCount}");
        return true;
    }

    public async Task<CurrentUserLiquidityDto> GetCurrentUserLiquidityAsync(GetCurrentUserLiquidityDto input)
    {
        var currentUserLiquidityIndex = await GetCurrentUserLiquidityIndexAsync(input.TradePairId, input.Address, _portfolioOptions.Value.DataVersion);
        var currentUserLiquidityDto =  _objectMapper.Map<CurrentUserLiquidityIndex, CurrentUserLiquidityDto>(currentUserLiquidityIndex);
        if (currentUserLiquidityDto.TradePairId != Guid.Empty)
        {
            var tradePairIndex = await _tradePairIndexRepository.GetAsync(currentUserLiquidityDto.TradePairId);
            currentUserLiquidityDto.TradePair = _objectMapper.Map<TradePair, TradePairWithTokenDto>(tradePairIndex);
        }
        return currentUserLiquidityDto;
    }
}