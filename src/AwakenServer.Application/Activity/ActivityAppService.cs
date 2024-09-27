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
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.Activity;
using AwakenServer.Price;
using AwakenServer.Price.Dtos;
using AwakenServer.Tokens;
using AwakenServer.Trade.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.EventBus.Distributed;


namespace AwakenServer.Activity;

[RemoteService(IsEnabled = false)]
public class ActivityAppService : ApplicationService, IActivityAppService
{
    private INESTRepository<JoinRecordIndex, Guid> _joinRecordRepository;
    private INESTRepository<UserActivityInfoIndex, Guid> _userActivityInfoRepository;
    private INESTRepository<RankingListSnapshotIndex, Guid> _rankingListSnapshotRepository;
    private IClusterClient _clusterClient;
    private IDistributedEventBus _distributedEventBus;
    private readonly ILogger<ActivityAppService> _logger;
    private readonly ActivityOptions _activityOptions;
    private readonly ITokenAppService _tokenAppService;
    private readonly IPriceAppService _priceAppService;

    protected const string VolumeActivityType = "volume";
    protected const string TvlActivityType = "tvl";
    protected const double LabsFeeRate = 0.0015;

    public ActivityAppService(INESTRepository<JoinRecordIndex, Guid> joinRecordRepository, 
        INESTRepository<UserActivityInfoIndex, Guid> userActivityInfoRepository, 
        INESTRepository<RankingListSnapshotIndex, Guid> rankingListSnapshotRepository, 
        IClusterClient clusterClient, IDistributedEventBus distributedEventBus, 
        ILogger<ActivityAppService> logger, 
        IOptionsSnapshot<ActivityOptions> activityOptions, 
        ITokenAppService tokenAppService, 
        IPriceAppService priceAppService)
    {
        _joinRecordRepository = joinRecordRepository;
        _userActivityInfoRepository = userActivityInfoRepository;
        _rankingListSnapshotRepository = rankingListSnapshotRepository;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _logger = logger;
        _activityOptions = activityOptions.Value;
        _tokenAppService = tokenAppService;
        _priceAppService = priceAppService;
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

        var joinRecordGrain = _clusterClient.GetGrain<IJoinRecordGrain>(GrainIdHelper.GenerateGrainId(input.ActivityId, input.Address));
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
                TotalPoint = rankingInfo.TotalPoint,
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

            return new DateTime(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, 0, 0);;
        }
        
        public DateTime GetNormalSnapshotTime(DateTime time)
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

        public bool IsActivityPool(Activity activity, SwapRecordDto dto)
        {
            if (dto.SwapRecords.Count > 0)
            {
                return false;
            }

            foreach (var pair in activity.TradePairs)
            {
                var activityPool = pair.Split('_').ToList();
                if (activityPool.Count == 2 &&
                    (activityPool[0] == dto.SymbolIn && activityPool[1] == dto.SymbolOut)
                    || (activityPool[0] == dto.SymbolOut && activityPool[1] == dto.SymbolIn))
                {
                    return true;
                }             
            }
            
            return false;
            
        }
        
        public async Task<bool> CreateSwapAsync(SwapRecordDto dto)
        {
            foreach (var activity in _activityOptions.ActivityList)
            {
                if (activity.Type == VolumeActivityType)
                {
                    if (!IsActivityPool(activity, dto))
                    {
                        continue;
                    }
                    if (dto.Timestamp >= activity.BeginTime && dto.Timestamp <= activity.EndTime)
                    {
                        // update user point
                        var point = await GetPointAsync(dto);
                        var userActivityGrainId = GrainIdHelper.GenerateGrainId(dto.ChainId, activity.Type, activity.ActivityId, dto.Sender);
                        var userActivityGrain = _clusterClient.GetGrain<IUserActivityGrain>(userActivityGrainId);
                        var userActivityResult = await userActivityGrain.GetAsync();
                        var isNewUser = !userActivityResult.Success;
                        userActivityResult = await userActivityGrain.AddUserPointAsync(dto.Sender, point, dto.Timestamp);
                        await _distributedEventBus.PublishAsync(
                            ObjectMapper.Map<UserActivityInfo, UserActivityInfoEto>(userActivityResult.Data));
                        
                        // update ranking
                        var currentActivityRankingGrainId = GrainIdHelper.GenerateGrainId( activity.Type, activity.ActivityId);
                        var currentActivityRankingGrain = _clusterClient.GetGrain<ICurrentActivityRankingGrain>(currentActivityRankingGrainId);
                        var currentActivityRankingResult = await currentActivityRankingGrain.AddOrUpdateAsync(userActivityResult.Data.Address, 
                            userActivityResult.Data.TotalPoint, userActivityResult.Data.LastUpdateTime, activity.ActivityId, isNewUser);
                        
                        // ranking snapshot
                        var snapshotTime = GetNormalSnapshotTime(DateTimeHelper.FromUnixTimeMilliseconds(dto.Timestamp));
                        var activityRankingSnapshotGrainId = GrainIdHelper.GenerateGrainId(activity.Type, activity.ActivityId, snapshotTime);
                        var activityRankingSnapshotGrain = _clusterClient.GetGrain<IActivityRankingSnapshotGrain>(activityRankingSnapshotGrainId);
                        currentActivityRankingResult.Data.Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(snapshotTime);
                        var activityRankingSnapshotResult = await activityRankingSnapshotGrain.AddOrUpdateAsync(currentActivityRankingResult.Data);
                        await _distributedEventBus.PublishAsync(
                        ObjectMapper.Map<RankingListSnapshot, RankingListSnapshotEto>(activityRankingSnapshotResult.Data));
                        
                    }
                }
            }
            return true;
        }

        public async Task<bool> CreateLpSnapshotAsync(DateTime executeTime)
        {
            // todo
            return true;
        }
        
}
