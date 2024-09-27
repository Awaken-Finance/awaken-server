using System;
using System.Collections.Generic;
using System.Linq;
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
    private INESTRepository<JoinRecordIndex, Guid> _joinRecordRepository;
    private INESTRepository<UserActivityInfoIndex, Guid> _userActivityInfoRepository;
    private INESTRepository<RankingListSnapshotIndex, Guid> _rankingListSnapshotRepository;
    private INESTRepository<CurrentUserLiquidityIndex, Guid> _currentUserLiquidityIndexRepository;
    private readonly INESTRepository<TradePair, Guid> _tradePairIndexRepository;
    private readonly IDistributedCache<string> _syncedTransactionIdCache;


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
    private const double LabsFeeRate = 0.0015;

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
    }

    private string AddVersionToKey(string baseKey, string version)
    {
        return $"{baseKey}:{version}";
    }

    public async Task JoinAsync(JoinInput input)
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
        var dataByte = HashHelper.ComputeFrom(input.Message).ToByteArray();
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
        var activityRankingSnapshotGrainId = GrainIdHelper.GenerateGrainId(activity.Type, activity.ActivityId, snapshotTime);
        var activityRankingSnapshotGrain = _clusterClient.GetGrain<IActivityRankingSnapshotGrain>(activityRankingSnapshotGrainId);
        currentActivityRankingResult.Data.Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(snapshotTime);
        var activityRankingSnapshotResult = await activityRankingSnapshotGrain.AddOrUpdateAsync(currentActivityRankingResult.Data);
        await _distributedEventBus.PublishAsync(
            ObjectMapper.Map<RankingListSnapshot, RankingListSnapshotEto>(activityRankingSnapshotResult.Data));
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

    private async Task<RankingListSnapshotIndex> GetLatestRankingListSnapshotAsync(int activity, DateTime maxTime)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<RankingListSnapshotIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ActivityId).Value(activity)));
        mustQuery.Add(q =>
            q.Range(i => i.Field(f => f.Timestamp).LessThanOrEquals(DateTimeHelper.ToUnixTimeMilliseconds(maxTime))));
        QueryContainer Filter(QueryContainerDescriptor<RankingListSnapshotIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _rankingListSnapshotRepository.GetAsync(Filter);
    }

    public async Task<JoinStatusDto> GetJoinStatusAsync(GetJoinStatusInput input)
    {
        var joinRecordExisted = await GetJoinRecordAsync(input.ActivityId, input.Address);
        var rankingListSnapshotIndex = await GetLatestRankingListSnapshotAsync(input.ActivityId, DateTime.UtcNow);
        return new JoinStatusDto
        {
            Status = joinRecordExisted == null ? 0 : 1,
            NumberOfJoin = rankingListSnapshotIndex?.NumOfJoin ?? 0
        };
    }


    public async Task<MyRankingDto> GetMyRankingAsync(GetMyRankingInput input)
    {
        var rankingListSnapshotIndex = await GetLatestRankingListSnapshotAsync(input.ActivityId, DateTime.UtcNow);
        var userActivityInfoIndex = await GetUserActivityInfoAsync(input.ActivityId, input.Address);
        var myRanking = 51;
        if (rankingListSnapshotIndex?.RankingList.Count > 0)
        {
            var index = rankingListSnapshotIndex.RankingList.FindIndex(t => t.Address == input.Address);
            if (index > 0)
            {
                myRanking = index + 1;
            }
        }

        return new MyRankingDto
        {
            Ranking = myRanking,
            TotalPoint = userActivityInfoIndex?.TotalPoint != null
                ? (long)userActivityInfoIndex.TotalPoint
                : 0
        };
    }

    public async Task<RankingListDto> GetRankingListAsync(ActivityBaseDto input)
    {
        var rankingListSnapshotIndex = await GetLatestRankingListSnapshotAsync(input.ActivityId, DateTime.UtcNow);
        if (rankingListSnapshotIndex == null)
        {
            return new RankingListDto();
        }

        var lastHourRankingListSnapshotIndex =
            await GetLatestRankingListSnapshotAsync(input.ActivityId, DateTime.UtcNow.AddHours(-1));
        var rankingInfoDtoList = new List<RankingInfoDto>();
        var ranking = 0;
        foreach (var rankingInfo in rankingListSnapshotIndex.RankingList)
        {
            ranking++;
            var rankingInfoDto = new RankingInfoDto()
            {
                Ranking = ranking,
                Address = rankingInfo.Address,
                TotalPoint = (long)rankingInfo.TotalPoint,
            };
            rankingInfoDtoList.Add(rankingInfoDto);
            if (lastHourRankingListSnapshotIndex == null ||
                lastHourRankingListSnapshotIndex.Id == rankingListSnapshotIndex.Id)
            {
                rankingInfoDto.NewStatus = 1;
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
            Items = rankingInfoDtoList
        };
    }

    private DateTime GetLpSnapshotTime(DateTime timestamp)
    {
        if (timestamp.Minute <= 10)
        {
            return new DateTime(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, 0, 0);
        }

        if (timestamp.Minute >= 50)
        {
            DateTime nextHour = timestamp.AddHours(1);
            return new DateTime(nextHour.Year, nextHour.Month, nextHour.Day, nextHour.Hour, 0, 0);
        }

        return new DateTime(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, 0, 0);
        ;
    }

    private DateTime GetNormalSnapshotTime(DateTime time)
    {
        return time.Date.AddHours(time.Hour);
    }

    private async Task<double> GetPointAsync(SwapRecordDto dto)
    {
        var labsFeeToken = await _tokenAppService.GetAsync(new GetTokenInput()
        {
            Symbol = dto.LabsFeeSymbol
        });
        var labsFee = dto.LabsFee / Math.Pow(10, labsFeeToken.Decimals);
        var labsFeeTokenPrice = await _priceAppService.GetTokenHistoryPriceDataAsync(
            new GetTokenHistoryPriceInput()
            {
                Symbol = dto.LabsFeeSymbol,
                DateTime = DateTimeHelper.FromUnixTimeMilliseconds(dto.Timestamp)
            }
        );
        var labsFeeInUsd = labsFee * (double)labsFeeTokenPrice.PriceInUsd;
        return labsFeeInUsd / LabsFeeRate;
    }

    private async Task<bool> IsActivityPoolAsync(Activity activity, SwapRecordDto dto)
    {
        if (dto.SwapRecords != null && dto.SwapRecords.Count > 0)
        {
            return false;
            
        }

        if (!_activityTradePairAddresses.ContainsKey(activity.ActivityId))
        {
            var activityPools = await GetActivityPair(activity);
            _activityTradePairAddresses.Add(activity.ActivityId, activityPools);
        }

        var activityPoolSet = new HashSet<string>(_activityTradePairAddresses[activity.ActivityId].Select(t => t.PairAddress));
        return activityPoolSet.Contains(dto.PairAddress);
    }

    private async Task UpdateUserPointAndRankingAsync(UpdatePointType updatePointType, string chainId, long timestamp, DateTime snapshotTime, Activity activity, string userAddress, double point)
    {
        _logger.LogInformation($"Update user point and ranking, updatePointType: {updatePointType}, snapshotTime: {snapshotTime}, activityId: {activity.ActivityId}, userAddress: {userAddress}, point: {point}");
        // update user point
        var userActivityGrainId =
            GrainIdHelper.GenerateGrainId(chainId, activity.Type, activity.ActivityId, userAddress);
        var userActivityGrain = _clusterClient.GetGrain<IUserActivityGrain>(userActivityGrainId);
        var userActivityResult = await userActivityGrain.GetAsync();
        var isNewUser = !userActivityResult.Success;
        switch (updatePointType)
        {
            case UpdatePointType.Update:
            {
                userActivityResult = await userActivityGrain.UpdateUserPointAsync(activity.ActivityId, userAddress, point, timestamp);
                break;
            }
            case UpdatePointType.Add:
            {
                userActivityResult = await userActivityGrain.AccumulateUserPointAsync(activity.ActivityId, userAddress, point, timestamp);
                break;
            }
        }
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
        var activityRankingSnapshotGrainId = GrainIdHelper.GenerateGrainId(activity.Type,
            activity.ActivityId, snapshotTime);
        var activityRankingSnapshotGrain =
            _clusterClient.GetGrain<IActivityRankingSnapshotGrain>(activityRankingSnapshotGrainId);
        currentActivityRankingResult.Data.Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(snapshotTime);
        var activityRankingSnapshotResult =
            await activityRankingSnapshotGrain.AddOrUpdateAsync(currentActivityRankingResult.Data);
        await _distributedEventBus.PublishAsync(
            ObjectMapper.Map<RankingListSnapshot, RankingListSnapshotEto>(
                activityRankingSnapshotResult.Data));
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
                if (!await IsActivityPoolAsync(activity, dto))
                {
                    continue;
                }

                if (dto.Timestamp >= activity.BeginTime && dto.Timestamp <= activity.EndTime)
                {
                    var point = await GetPointAsync(dto);
                    var snapshotTime = GetNormalSnapshotTime(DateTimeHelper.FromUnixTimeMilliseconds(dto.Timestamp));
                    await UpdateUserPointAndRankingAsync(UpdatePointType.Add, dto.ChainId, dto.Timestamp, snapshotTime, activity, dto.Sender, point);
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
        mustQuery.Add(q => q.Range(i => i.Field(f => f.LpTokenAmount).GreaterThan(0)));
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

    public async Task<bool> CreateLpSnapshotAsync(long executeTime)
    {
        foreach (var activity in _activityOptions.ActivityList)
        {
            if (executeTime >= activity.BeginTime && executeTime <= activity.EndTime)
            {
                if (activity.Type == TvlActivityType)
                {
                    if (!_activityTradePairAddresses.ContainsKey(activity.ActivityId))
                    {
                        var activityPools = await GetActivityPair(activity);
                        _activityTradePairAddresses.Add(activity.ActivityId, activityPools);
                    }

                    var activityPairs = _activityTradePairAddresses[activity.ActivityId];
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

                            var point = lpTokenPercentage * pair.TVL;
                            var snapshotTime = GetLpSnapshotTime(DateTimeHelper.FromUnixTimeMilliseconds(executeTime));

                            await UpdateUserPointAndRankingAsync(UpdatePointType.Update, userPairLiquidity.ChainId, executeTime, snapshotTime, activity, userPairLiquidity.Address, point);
                        }
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