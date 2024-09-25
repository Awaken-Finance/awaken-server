using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AElf;
using AElf.Cryptography;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Activity.Dtos;
using AwakenServer.Activity.Eto;
using AwakenServer.Activity.Index;
using Nest;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
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
    public async Task JoinAsync(JoinInput input)
    {
        // check activity in progress
        var joinRecordExisted = await GetJoinRecordAsync(input.ActivityId, input.Address);
        if (joinRecordExisted != null)
        {
            throw new UserFriendlyException("Join already");
        }

        var publicKeyByte = ByteArrayHelper.HexStringToByteArray(input.PublicKey);
        var dataByte = ByteArrayHelper.HexStringToByteArray(input.Message);
        var signatureByte = ByteArrayHelper.HexStringToByteArray(input.Signature);
        if (!CryptoHelper.VerifySignature(signatureByte, dataByte, publicKeyByte))
        {
            throw new UserFriendlyException("Verify signature fail");
        }
        // grain add join record
        await _distributedEventBus.PublishAsync(new JoinRecordEto());
    }
    
    public async Task<JoinRecordIndex> GetJoinRecordAsync(int activity, string address)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<JoinRecordIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ActivityId).Value(activity)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(address)));
        QueryContainer Filter(QueryContainerDescriptor<JoinRecordIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _joinRecordRepository.GetAsync(Filter);
    }
    
    public async Task<UserActivityInfoIndex> GetUserActivityInfoAsync(int activity, string address)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserActivityInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ActivityId).Value(activity)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(address)));
        QueryContainer Filter(QueryContainerDescriptor<UserActivityInfoIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _userActivityInfoRepository.GetAsync(Filter);
    }
    
    public async Task<RankingListSnapshotIndex> GetLatestRankingListSnapshotAsync(int activity, DateTime maxTime)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<RankingListSnapshotIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ActivityId).Value(activity)));
        mustQuery.Add(q => q.Range(i => i.Field(f => f.Timestamp).LessThanOrEquals(DateTimeHelper.ToUnixTimeMilliseconds(maxTime))));
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
        return new MyRankingDto{
            Ranking = myRanking,
            TotalPoint = userActivityInfoIndex?.TotalPoint ?? 0
        };
    }

    public async Task<RankingListDto> GetRankingListAsync(ActivityBaseDto input)
    {
        var rankingListSnapshotIndex = await GetLatestRankingListSnapshotAsync(input.ActivityId, DateTime.UtcNow);
        if (rankingListSnapshotIndex == null)
        {
            return new RankingListDto();
        }

        var lastHourRankingListSnapshotIndex = await GetLatestRankingListSnapshotAsync(input.ActivityId, DateTime.UtcNow.AddHours(-1));
        var rankingInfoDtoList = new List<RankingInfoDto>();
        foreach (var rankingInfo in rankingListSnapshotIndex.RankingList)
        {
            var rankingInfoDto = new RankingInfoDto()
            {
                Ranking = rankingInfo.Ranking,
                Address = rankingInfo.Address,
                TotalPoint = rankingInfo.TotalPoint,
            };
            rankingInfoDtoList.Add(rankingInfoDto);
            if (lastHourRankingListSnapshotIndex == null || lastHourRankingListSnapshotIndex.Id == rankingListSnapshotIndex.Id)
            {
                rankingInfoDto.NewStatus = 1;
                continue;
            }
            var lastHourRankingInfo = lastHourRankingListSnapshotIndex.RankingList.Find(t => t.Address == rankingInfo.Address);
            if (lastHourRankingInfo == null)
            {
                rankingInfoDto.NewStatus = 1;
                continue;
            }
            rankingInfoDto.RankingChange1H = lastHourRankingInfo.Ranking - rankingInfo.Ranking;
        }

        return new RankingListDto
        {
            Items = rankingInfoDtoList
        };
    }
}