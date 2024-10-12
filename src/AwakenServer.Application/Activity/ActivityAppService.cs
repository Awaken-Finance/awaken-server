using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf;
using AElf.Cryptography;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Activity.Dtos;
using AwakenServer.Activity.Eto;
using AwakenServer.Activity.Index;
using AwakenServer.Asset;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.Activity;
using AwakenServer.Grains.Grain.MyPortfolio;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Price;
using AwakenServer.Price.Dtos;
using AwakenServer.Provider;
using AwakenServer.Tokens;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Index;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Volo.Abp.EventBus.Distributed;


namespace AwakenServer.Activity;

[RemoteService(IsEnabled = false)]
public class ActivityAppService : ApplicationService, IActivityAppService
{
    private const string SyncedTransactionCachePrefix = "ActivitySynced";
    private const string SyncedLimitFillRecordTransactionCachePrefix = "ActivityLimitFillRecordSynced";
    private const int MaxRankingCount = 50;
    private INESTRepository<JoinRecordIndex, Guid> _joinRecordRepository;
    private INESTRepository<UserActivityInfoIndex, Guid> _userActivityInfoRepository;
    private INESTRepository<RankingListSnapshotIndex, Guid> _rankingListSnapshotRepository;
    private INESTRepository<CurrentUserLiquidityIndex, Guid> _currentUserLiquidityIndexRepository;
    private readonly INESTRepository<TradePair, Guid> _tradePairIndexRepository;
    private readonly IDistributedCache<string> _syncedTransactionIdCache;
    protected readonly IGraphQLProvider _graphQlProvider;



    private IClusterClient _clusterClient;
    private IDistributedEventBus _distributedEventBus;
    private readonly ILogger<ActivityAppService> _logger;
    private readonly ActivityOptions _activityOptions;
    private readonly PortfolioOptions _portfolioOptions;

    private readonly ITokenAppService _tokenAppService;
    private readonly IPriceAppService _priceAppService;

    private Dictionary<int, List<ActivityTradePair>> _activityTradePairAddresses;

    private const string VolumeActivityType = "volume";
    private const string TvlActivityType = "tvl";
    private const long TvlActivityInitNum = 1000;
    private const double LabsFeeRate = 0.0015;
    private const double LimitLabsFeeRate = 0.0005;

    public ActivityAppService(
        ILogger<ActivityAppService> logger,
        IOptionsSnapshot<ActivityOptions> activityOptions,
        IClusterClient clusterClient,
        IPriceAppService priceAppService,
        ITokenAppService tokenAppService,
        IOptionsSnapshot<PortfolioOptions> portfolioOptions,
        INESTRepository<CurrentUserLiquidityIndex, Guid> currentUserLiquidityIndexRepository,
        INESTRepository<JoinRecordIndex, Guid> joinRecordRepository,
        INESTRepository<UserActivityInfoIndex, Guid> userActivityInfoRepository,
        INESTRepository<RankingListSnapshotIndex, Guid> rankingListSnapshotRepository,
        INESTRepository<TradePair, Guid> tradePairIndexRepository,
        IDistributedEventBus distributedEventBus,
        IGraphQLProvider graphQlProvider,
        IDistributedCache<string> syncedTransactionIdCache)
    {
        _logger = logger;
        _activityOptions = activityOptions.Value;
        _clusterClient = clusterClient;
        _tokenAppService = tokenAppService;
        _priceAppService = priceAppService;
        _distributedEventBus = distributedEventBus;
        _currentUserLiquidityIndexRepository = currentUserLiquidityIndexRepository;
        _activityTradePairAddresses = new Dictionary<int, List<ActivityTradePair>>();
        _portfolioOptions = portfolioOptions.Value;
        _tradePairIndexRepository = tradePairIndexRepository;
        _joinRecordRepository = joinRecordRepository;
        _userActivityInfoRepository = userActivityInfoRepository;
        _rankingListSnapshotRepository = rankingListSnapshotRepository;
        _syncedTransactionIdCache = syncedTransactionIdCache;
        _graphQlProvider = graphQlProvider;
    }

    private string AddVersionToKey(string baseKey, string version)
    {
        return $"{baseKey}:{version}";
    }

    public async Task<string> JoinAsync(JoinInput input)
    {
        var activity = _activityOptions.ActivityList.Find(t => t.ActivityId == input.ActivityId);
        if (activity == null)
        {
            throw new UserFriendlyException("Activity not existed");
        }

        if (activity.EndTime < DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow))
        {
            throw new UserFriendlyException("Activity has ended");
        }
        var joinRecordExisted = await GetJoinRecordAsync(input.ActivityId, input.Address);
        if (joinRecordExisted != null)
        {
            throw new UserFriendlyException("Join already");
        }

        var publicKeyByte = ByteArrayHelper.HexStringToByteArray(input.PublicKey);
        var messageHash = ByteExtensions.ToHex(Encoding.UTF8.GetBytes(input.Message));
        var dataByte = HashHelper.ComputeFrom(messageHash).ToByteArray();
        var signatureByte = ByteArrayHelper.HexStringToByteArray(input.Signature);
        if (!CryptoHelper.VerifySignature(signatureByte, dataByte, publicKeyByte))
        {
            throw new UserFriendlyException("Verify signature fail");
        }

        var joinRecordGrain =
            _clusterClient.GetGrain<IJoinRecordGrain>(GrainIdHelper.GenerateGrainId(input.ActivityId, input.Address));
        var joinRecordGrainResult = await joinRecordGrain.AddOrUpdateAsync(new JoinRecordGrainDto
        {
            Address = input.Address,
            ActivityId = input.ActivityId,
            Signature = input.Signature,
            Message = input.Message,
            PublicKey = input.PublicKey
        });
        if (!joinRecordGrainResult.Success)
        {
            throw new UserFriendlyException("Join already");
        }

        await _distributedEventBus.PublishAsync(
            ObjectMapper.Map<JoinRecord, JoinRecordEto>(joinRecordGrainResult.Data));
        await _distributedEventBus.PublishAsync(ObjectMapper.Map<JoinRecord, JoinRecordEto>(joinRecordGrainResult.Data));

        var currentActivityRankingGrainId = GrainIdHelper.GenerateGrainId(activity.Type, activity.ActivityId);
        var currentActivityRankingGrain = _clusterClient.GetGrain<ICurrentActivityRankingGrain>(currentActivityRankingGrainId);
        var currentActivityRankingResult = await currentActivityRankingGrain.AddNumOfPointAsync(activity.ActivityId,1);
        
        // ranking snapshot
        var snapshotTime = GetNormalSnapshotTime(DateTime.UtcNow);
        var snapshotTimeStamp = DateTimeHelper.ToUnixTimeMilliseconds(snapshotTime);
        var activityRankingSnapshotGrainId = GrainIdHelper.GenerateGrainId(activity.Type, activity.ActivityId, snapshotTimeStamp);
        var activityRankingSnapshotGrain = _clusterClient.GetGrain<IActivityRankingSnapshotGrain>(activityRankingSnapshotGrainId);
        currentActivityRankingResult.Data.Timestamp = snapshotTimeStamp;
        var activityRankingSnapshotResult = await activityRankingSnapshotGrain.AddOrUpdateAsync(currentActivityRankingResult.Data);
        await _distributedEventBus.PublishAsync(
            ObjectMapper.Map<RankingListSnapshot, RankingListSnapshotEto>(activityRankingSnapshotResult.Data));
        return "Success";
    }

    private async Task<JoinRecordIndex> GetJoinRecordAsync(int activity, string address)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<JoinRecordIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ActivityId).Value(activity)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(address)));
        QueryContainer Filter(QueryContainerDescriptor<JoinRecordIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _joinRecordRepository.GetAsync(Filter);
    }

    private async Task<UserActivityInfoIndex> GetUserActivityInfoAsync(int activity, string address)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserActivityInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ActivityId).Value(activity)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(address)));
        QueryContainer Filter(QueryContainerDescriptor<UserActivityInfoIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _userActivityInfoRepository.GetAsync(Filter);
    }

    private async Task<RankingListSnapshotIndex> GetLatestRankingListSnapshotAsync(int activityId, DateTime maxTime)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<RankingListSnapshotIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ActivityId).Value(activityId)));
        mustQuery.Add(q =>
            q.Range(i => i.Field(f => f.Timestamp).LessThanOrEquals(DateTimeHelper.ToUnixTimeMilliseconds(maxTime))));
        QueryContainer Filter(QueryContainerDescriptor<RankingListSnapshotIndex> f) => f.Bool(b => b.Must(mustQuery));
        var snapshotIndex = await _rankingListSnapshotRepository.GetAsync(Filter, sortExp: k => k.Timestamp, sortType: SortOrder.Descending);
        if (snapshotIndex == null)
        {
            return null;
        }
        var activityOption = _activityOptions.ActivityList.Find(t => t.ActivityId == activityId);
        if (activityOption?.WhiteList.Count > 0)
        {
            snapshotIndex.RankingList = snapshotIndex.RankingList
                .Where(t => !activityOption.WhiteList.Contains(t.Address)).ToList();
        }
        return snapshotIndex;
    }

    public async Task<JoinStatusDto> GetJoinStatusAsync(GetJoinStatusInput input)
    {
        var joinRecordExisted = await GetJoinRecordAsync(input.ActivityId, input.Address);
        var rankingListSnapshotIndex = await GetLatestRankingListSnapshotAsync(input.ActivityId, DateTime.UtcNow);
        var activity = _activityOptions.ActivityList.Find(t => t.ActivityId == input.ActivityId);
        var numberOfJoin = rankingListSnapshotIndex?.NumOfJoin ?? 0;
        if (activity?.Type == TvlActivityType)
        {
            numberOfJoin += TvlActivityInitNum;
        }
        return new JoinStatusDto
        {
            Status = joinRecordExisted == null ? 0 : 1,
            NumberOfJoin = numberOfJoin
        };
    }


    public async Task<MyRankingDto> GetMyRankingAsync(GetMyRankingInput input)
    {
        var activity = _activityOptions.ActivityList.Find(t => t.ActivityId == input.ActivityId);
        if (activity == null)
        {
            throw new UserFriendlyException("Activity not existed");
        }
        var userActivityInfoIndex = await GetUserActivityInfoAsync(input.ActivityId, input.Address);
        if (userActivityInfoIndex == null)
        {
            return new MyRankingDto
            {
                TotalPoint = "0"
            };
        }
        var rankingListSnapshotIndex = await GetLatestRankingListSnapshotAsync(input.ActivityId, DateTime.UtcNow);
        var myRanking = 1001;
        if (rankingListSnapshotIndex?.RankingList.Count > 0)
        {
            var index = rankingListSnapshotIndex.RankingList.FindIndex(t => t.Address == input.Address);
            if (index >= 0)
            {
                myRanking = index + 1;
            }
        }

        var totalPoint = activity.Type == VolumeActivityType
            ? userActivityInfoIndex.TotalPoint.ToString("0.00")
            : userActivityInfoIndex.TotalPoint.ToString("0");
        return new MyRankingDto
        {
            Ranking = myRanking,
            TotalPoint = totalPoint
        };
    }

    public async Task<RankingListDto> GetRankingListAsync(ActivityBaseDto input)
    {
        var activity = _activityOptions.ActivityList.Find(t => t.ActivityId == input.ActivityId);
        if (activity == null)
        {
            throw new UserFriendlyException("Activity not existed");
        }
        var rankingListSnapshotIndex = await GetLatestRankingListSnapshotAsync(input.ActivityId, DateTime.UtcNow);
        if (rankingListSnapshotIndex == null)
        {
            return new RankingListDto
            {
                ActivityId = input.ActivityId
            };
        }

        var lastHourRankingListSnapshotIndex =
            await GetLatestRankingListSnapshotAsync(input.ActivityId, DateTime.UtcNow.AddHours(-1));
        var rankingInfoDtoList = new List<RankingInfoDto>();
        var ranking = 0;
        foreach (var rankingInfo in rankingListSnapshotIndex.RankingList)
        {
            ranking++;
            if (ranking > MaxRankingCount)
            {
                break;
            }
            var totalPoint = activity.Type == VolumeActivityType
                ? rankingInfo.TotalPoint.ToString("0.00")
                : rankingInfo.TotalPoint.ToString("0");
            var rankingInfoDto = new RankingInfoDto()
            {
                Ranking = ranking,
                Address = rankingInfo.Address,
                TotalPoint = totalPoint,
            };
            rankingInfoDtoList.Add(rankingInfoDto);
            if (lastHourRankingListSnapshotIndex == null)
            {
                rankingInfoDto.NewStatus = 1;
                continue;
            }
            if (lastHourRankingListSnapshotIndex.Id == rankingListSnapshotIndex.Id)
            {
                continue;
            }

            var lastHourRankingInfoRanking =
                lastHourRankingListSnapshotIndex.RankingList.FindIndex(t => t.Address == rankingInfo.Address) + 1;
            if (lastHourRankingInfoRanking == 0)
            {
                rankingInfoDto.NewStatus = 1;
                continue;
            }
            rankingInfoDto.RankingChange1H = lastHourRankingInfoRanking - ranking;
        }

        return new RankingListDto
        {
            Items = rankingInfoDtoList,
            ActivityId = input.ActivityId
        };
    }

    private DateTime GetNormalSnapshotTime(DateTime time)
    {
        return time.Date.AddHours(time.Hour);
    }

    private async Task<double> GetTokenValueInUsdAsync(string tokenSymbol, long amountWithDecimal, long timestamp)
    {
        var token = await _tokenAppService.GetAsync(new GetTokenInput()
        {
            Symbol = tokenSymbol
        });
        var amount = amountWithDecimal / Math.Pow(10, token.Decimals);
        var tokenPrice = await _priceAppService.GetTokenHistoryPriceDataAsync(
            new GetTokenHistoryPriceInput()
            {
                Symbol = tokenSymbol,
                DateTime = DateTimeHelper.FromUnixTimeMilliseconds(timestamp)
            }
        );
        return amount * (double)tokenPrice.PriceInUsd;
    }
    
    private async Task<double> GetPointAsync(SwapRecordDto dto)
    {
        if (dto.LabsFee <= 0)
        {
            return 0d;
        }
        
        var labsFeeInUsd = await GetTokenValueInUsdAsync(dto.LabsFeeSymbol, dto.LabsFee, dto.Timestamp);
        var swapValueFromLabsFee = labsFeeInUsd / LabsFeeRate;
        
        _logger.LogInformation($"Get trade swap point, txn: {dto.TransactionHash}, labsFeeSymbol: {dto.LabsFeeSymbol}, swapValueFromLabsFee: {swapValueFromLabsFee}");
        
        var pricingTokensSet = new HashSet<string>(_activityOptions.PricingTokens);
        if (pricingTokensSet.Contains(dto.LabsFeeSymbol))
        {
            _logger.LogInformation($"Get trade swap point, txn: {dto.TransactionHash}, from labs fee token, symbol: {dto.LabsFeeSymbol}, final point: {swapValueFromLabsFee}");
            return swapValueFromLabsFee;
        }

        var swapValueFromPricingTokenMap = new Dictionary<string, double>();
        foreach (var swapRecord in dto.SwapRecords)
        {
            // check symbol out is special token and add value
            if (pricingTokensSet.Contains(swapRecord.SymbolIn))
            {
                var swapValueFromPricingToken = await GetTokenValueInUsdAsync(swapRecord.SymbolIn, swapRecord.AmountIn, dto.Timestamp);
                if (!swapValueFromPricingTokenMap.ContainsKey(swapRecord.SymbolIn))
                {
                    swapValueFromPricingTokenMap.Add(swapRecord.SymbolIn, 0);
                }
                swapValueFromPricingTokenMap[swapRecord.SymbolIn] += swapValueFromPricingToken;
            }
        }

        var finalPoint = swapValueFromLabsFee;
        if (swapValueFromPricingTokenMap.Values != null && swapValueFromPricingTokenMap.Values.Count > 0)
        {
            finalPoint = Math.Min(swapValueFromPricingTokenMap.Values.Min(), finalPoint);
        }
        
        foreach (var swapValueFromPricingToken in swapValueFromPricingTokenMap)
        {
            _logger.LogInformation($"Get trade swap point, txn: {dto.TransactionHash}, pricing token: {swapValueFromPricingToken.Key}, swapValue: {swapValueFromPricingToken.Value}");
        }
        
        _logger.LogInformation($"Get trade swap point, txn: {dto.TransactionHash}, from pricing token, final point: {finalPoint}");
        return finalPoint;
    }
    
    private async Task<double> GetPointAsync(LimitOrderFillRecordDto dto)
    {
        if (dto.TotalFee <= 0)
        {
            return 0d;
        }
        
        var labsFeeInUsd = await GetTokenValueInUsdAsync(dto.SymbolOut, dto.TotalFee, dto.TransactionTime);
        var swapValueFromLabsFee = labsFeeInUsd / LimitLabsFeeRate;
        
        var pricingTokensSet = new HashSet<string>(_activityOptions.PricingTokens);
        if (pricingTokensSet.Contains(dto.SymbolOut))
        {
            _logger.LogInformation($"Get trade limit fill record point, txn: {dto.TransactionHash}, symbol: {dto.SymbolOut}, swapValueFromLabsFee: {swapValueFromLabsFee}");
            return swapValueFromLabsFee;
        }

        var swapValueFromPricingToken = await GetTokenValueInUsdAsync(dto.SymbolIn, dto.AmountInFilled, dto.TransactionTime);
        var finalPoint = Math.Min(swapValueFromLabsFee, swapValueFromPricingToken);
        _logger.LogInformation($"Get trade limit fill record point, txn: {dto.TransactionHash}, swapValueFromLabsFee: {swapValueFromLabsFee}, swapValueFromPricingToken: {swapValueFromPricingToken}, final point: {finalPoint}");
        return finalPoint;
    }

    private async Task<bool> BelongsAvtivityAsync(SwapRecord swapRecord, Activity activity)
    {
        foreach (var activityTradePair in activity.TradePairs)
        {
            var activityPool = activityTradePair.Split('_').ToList();
            if (activityPool.Count != 2)
            {
                continue;
            }
            if (swapRecord.SymbolIn == activityPool[0] && swapRecord.SymbolOut == activityPool[1]
                || swapRecord.SymbolIn == activityPool[1] && swapRecord.SymbolOut == activityPool[0])
            {
                return true;
            }
        }

        return false;
    }
    
    private async Task<bool> ContainsActivityAsync(SwapRecordDto dto, Activity activity)
    {
        if (dto.SwapRecords != null)
        {
            foreach (var swapRecord in dto.SwapRecords)
            {
                if (await BelongsAvtivityAsync(swapRecord, activity))
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    private async Task<bool> BelongsAvtivityAsync(LimitOrderFillRecordDto record, Activity activity)
    {
        foreach (var activityTradePair in activity.TradePairs)
        {
            var activityPool = activityTradePair.Split('_').ToList();
            if (activityPool.Count != 2)
            {
                continue;
            }
            if (record.SymbolIn == activityPool[0] && record.SymbolOut == activityPool[1]
                || record.SymbolIn == activityPool[1] && record.SymbolOut == activityPool[0])
            {
                return true;
            }
        }

        return false;
    }

    private async Task UpdateUserPointAndRankingAsync(string chainId, long timestamp, DateTime snapshotTime, Activity activity, string userAddress, double point, string type)
    {
        _logger.LogInformation($"Update user point and ranking by: {type}, updatePointType: snapshotTime: {snapshotTime}, activityId: {activity.ActivityId}, userAddress: {userAddress}, point: {point}");
        // update user point
        var userActivityGrainId =
            GrainIdHelper.GenerateGrainId(chainId, activity.Type, activity.ActivityId, userAddress);
        var userActivityGrain = _clusterClient.GetGrain<IUserActivityGrain>(userActivityGrainId);
        var userActivityResult = await userActivityGrain.GetAsync();
        var isNewUser = !userActivityResult.Success;
        userActivityResult = await userActivityGrain.AccumulateUserPointAsync(activity.ActivityId, userAddress, point, timestamp);
  
        await _distributedEventBus.PublishAsync(
            ObjectMapper.Map<UserActivityInfo, UserActivityInfoEto>(userActivityResult.Data));

        // update ranking
        var currentActivityRankingGrainId =
            GrainIdHelper.GenerateGrainId(activity.Type, activity.ActivityId);
        var currentActivityRankingGrain =
            _clusterClient.GetGrain<ICurrentActivityRankingGrain>(currentActivityRankingGrainId);
        var currentActivityRankingResult = await currentActivityRankingGrain.AddOrUpdateAsync(
            userActivityResult.Data.Address,
            userActivityResult.Data.TotalPoint, userActivityResult.Data.LastUpdateTime, activity.ActivityId,
            isNewUser);

        // ranking snapshot
        var snapshotTimeStamp = DateTimeHelper.ToUnixTimeMilliseconds(snapshotTime);
        var activityRankingSnapshotGrainId = GrainIdHelper.GenerateGrainId(activity.Type,
            activity.ActivityId, snapshotTimeStamp);
        var activityRankingSnapshotGrain =
            _clusterClient.GetGrain<IActivityRankingSnapshotGrain>(activityRankingSnapshotGrainId);
        currentActivityRankingResult.Data.Timestamp = snapshotTimeStamp;
        var activityRankingSnapshotResult =
            await activityRankingSnapshotGrain.AddOrUpdateAsync(currentActivityRankingResult.Data);
        await _distributedEventBus.PublishAsync(
            ObjectMapper.Map<RankingListSnapshot, RankingListSnapshotEto>(
                activityRankingSnapshotResult.Data));
    }

    public async Task<bool> CreateLimitOrderFillRecordAsync(LimitOrderFillRecordDto dto)
    {
        foreach (var activity in _activityOptions.ActivityList)
        {
            var key = $"{SyncedLimitFillRecordTransactionCachePrefix}:{dto.OrderId}:{dto.TransactionHash}:{activity.ActivityId}";
            var existed = await _syncedTransactionIdCache.GetAsync(key);
            if (!existed.IsNullOrWhiteSpace())
            {
                return false;
            }
            if (activity.Type == VolumeActivityType)
            {
                if (dto.TransactionTime >= activity.BeginTime && dto.TransactionTime <= activity.EndTime)
                {
                    
                    if (!await BelongsAvtivityAsync(dto, activity))
                    {
                        continue;
                    }
                    
                    var point = await GetPointAsync(dto);
                    var snapshotTime = GetNormalSnapshotTime(DateTimeHelper.FromUnixTimeMilliseconds(dto.TransactionTime));
                    await UpdateUserPointAndRankingAsync(dto.ChainId, dto.TransactionTime, snapshotTime, activity, dto.MakerAddress, point, "LimitMaker");
                }
            }
            
            await _syncedTransactionIdCache.SetAsync(key, "1", new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(14)
            });
        }
        return true;
    }
    
    public async Task<bool> CreateSwapAsync(SwapRecordDto dto)
    {
        foreach (var activity in _activityOptions.ActivityList)
        {
            var key = $"{SyncedTransactionCachePrefix}:{dto.TransactionHash}:{activity.ActivityId}";
            var existed = await _syncedTransactionIdCache.GetAsync(key);
            if (!existed.IsNullOrWhiteSpace())
            {
                return false;
            }
            if (activity.Type == VolumeActivityType)
            {
                if (dto.Timestamp >= activity.BeginTime && dto.Timestamp <= activity.EndTime)
                {
                    if (dto.SwapRecords == null)
                    {
                        dto.SwapRecords = new List<SwapRecord>();
                    }
                    
                    dto.SwapRecords.AddFirst(new Trade.Dtos.SwapRecord()
                    {
                        PairAddress = dto.PairAddress,
                        AmountIn = dto.AmountIn,
                        AmountOut = dto.AmountOut,
                        SymbolIn = dto.SymbolIn,
                        SymbolOut = dto.SymbolOut,
                        TotalFee = dto.TotalFee,
                        Channel = dto.Channel,
                        IsLimitOrder = dto.IsLimitOrder
                    });
                    
                    if (!await ContainsActivityAsync(dto, activity))
                    {
                        continue;
                    }
                    
                    var point = await GetPointAsync(dto);
                    var snapshotTime = GetNormalSnapshotTime(DateTimeHelper.FromUnixTimeMilliseconds(dto.Timestamp));
                    await UpdateUserPointAndRankingAsync(dto.ChainId, dto.Timestamp, snapshotTime, activity, dto.Sender, point, "Swap");
                }
            }
            
            await _syncedTransactionIdCache.SetAsync(key, "1", new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(14)
            });
        }
        return true;
    }

    private async Task<List<CurrentUserLiquidityIndex>> GetCurrentUserLiquidityIndexListAsync(Guid tradePairId, string dataVersion)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CurrentUserLiquidityIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.TradePairId).Value(tradePairId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Version).Value(dataVersion)));
        QueryContainer Filter(QueryContainerDescriptor<CurrentUserLiquidityIndex> f) => f.Bool(b => b.Must(mustQuery));
        var result = await _currentUserLiquidityIndexRepository.GetListAsync(Filter, skip: 0, limit: 10000);
        return result.Item2;
    }
    
    private async Task<List<ActivityTradePair>> GetActivityPair(Activity activity)
    {
        var result = new List<ActivityTradePair>();
        foreach (var activityTradePair in activity.TradePairs)
        {
            var activityPool = activityTradePair.Split('_').ToList();
            if (activityPool.Count != 2)
            {
                continue;
            }
            var mustQuery = new List<Func<QueryContainerDescriptor<TradePair>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));
            mustQuery.Add(q => q.Bool(b => b
                .Should(
                    q.Bool(b1 => b1
                        .Must(
                            q.Term(i => i.Field(f => f.Token0.Symbol).Value(activityPool[0])),
                            q.Term(i => i.Field(f => f.Token1.Symbol).Value(activityPool[1]))
                        )
                    ),
                    q.Bool(b1 => b1
                        .Must(
                            q.Term(i => i.Field(f => f.Token0.Symbol).Value(activityPool[1])),
                            q.Term(i => i.Field(f => f.Token1.Symbol).Value(activityPool[0]))
                        )
                    )
                )
            ));
            QueryContainer Filter(QueryContainerDescriptor<TradePair> f) => f.Bool(b => b.Must(mustQuery));
            var pairList = await _tradePairIndexRepository.GetListAsync(Filter);
            foreach (var pair in pairList.Item2)
            {
                _logger.LogInformation($"Activity: {activity.ActivityId}, pair: {activityTradePair}, find es pair: {pair.Address} - {pair.Id}");
                result.Add(new ActivityTradePair()
                {
                    PairAddress = pair.Address,
                    PairId = pair.Id
                });
            }
        }

        return result;
    }

    public async Task<bool> CreateLpSnapshotAsync(long executeTime, string type)
    {
        var snapshotTime = RandomSnapshotHelper.GetLpSnapshotTime(DateTimeHelper.FromUnixTimeMilliseconds(executeTime));
        foreach (var activity in _activityOptions.ActivityList)
        {
            if (activity.Type != TvlActivityType)
            {
                continue;
            }
            
            _logger.LogInformation($"Create LP snapshot request, " +
                                   $"from: {type}, " +
                                   $"activityId: {activity.ActivityId}, " +
                                   $"executeTime: {executeTime}, " +
                                   $"activity time: {activity.BeginTime}-{activity.EndTime}, " +
                                   $"whiteList: {string.Join(", ", activity.WhiteList)}," +
                                   $"pools: {string.Join(", ", activity.TradePairs)}");
            
            var snapshotTimeStamp = DateTimeHelper.ToUnixTimeMilliseconds(snapshotTime);
            var activityRankingSnapshotGrainId = GrainIdHelper.GenerateGrainId(activity.Type,
                activity.ActivityId, snapshotTimeStamp);
            var activityRankingSnapshotGrain =
                _clusterClient.GetGrain<IActivityRankingSnapshotGrain>(activityRankingSnapshotGrainId);
            var snapshotResult = await activityRankingSnapshotGrain.GetAsync();
            if (snapshotResult.Success && snapshotResult.Data.RankingList != null && snapshotResult.Data.RankingList.Count > 0)
            {
                _logger.LogInformation($"Create LP snapshot, {activityRankingSnapshotGrainId} already exist");
                continue;
            }
            
            if (executeTime >= activity.BeginTime && executeTime <= activity.EndTime)
            {
                if (!_activityTradePairAddresses.ContainsKey(activity.ActivityId))
                {
                    var activityPools = await GetActivityPair(activity);
                    _activityTradePairAddresses.Add(activity.ActivityId, activityPools);
                }
                var activityPairs = _activityTradePairAddresses[activity.ActivityId];
                _logger.LogInformation($"Create LP snapshot begin, " +
                                       $"from: {type}, " +
                                       $"activityId: {activity.ActivityId}, " +
                                       $"executeTime: {executeTime}, " +
                                       $"activity time: {activity.BeginTime}-{activity.EndTime}, " +
                                       $"whiteList: {string.Join(", ", activity.WhiteList)}," +
                                       $"pools: {string.Join(", ", activity.TradePairs)}");
                foreach (var activityPair in activityPairs)
                {
                    var pairLiquidity = await GetCurrentUserLiquidityIndexListAsync(activityPair.PairId, _portfolioOptions.DataVersion);
                    foreach (var userPairLiquidity in pairLiquidity)
                    {
                        var tradePairGrain = _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(userPairLiquidity.TradePairId));
                        var pair = (await tradePairGrain.GetAsync()).Data;
                        
                        var currentTradePairGrain = _clusterClient.GetGrain<ICurrentTradePairGrain>(AddVersionToKey(GrainIdHelper.GenerateGrainId(userPairLiquidity.TradePairId), _portfolioOptions.DataVersion));
                        var currentTradePair = (await currentTradePairGrain.GetAsync()).Data;
                        
                        var lpTokenPercentage = currentTradePair.TotalSupply == 0
                            ? 0.0
                            : userPairLiquidity.LpTokenAmount / (double)currentTradePair.TotalSupply;

                        var point = 100 * lpTokenPercentage * pair.TVL;
                        await UpdateUserPointAndRankingAsync(userPairLiquidity.ChainId, executeTime, snapshotTime, activity, userPairLiquidity.Address, point, "LP");
                    }
                }
            }
        }

        return true;
    }

    public class ActivityTradePair
    {
        public string PairAddress { get; set; }
        public Guid PairId { get; set; }
    }
    
    public enum UpdatePointType
    {
        Add = 1,
        Update
    }
}