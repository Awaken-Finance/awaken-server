using AwakenServer.Grains.State.Activity;
using AwakenServer.Grains.State.MyPortfolio;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Index;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Grains.Grain.Activity;

public class CurrentActivityRankingGrain : Grain<ActivityRankingSnapshotState>, ICurrentActivityRankingGrain
{
    private readonly IObjectMapper _objectMapper;
    public CurrentActivityRankingGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public async Task<GrainResultDto<ActivityRankingSnapshotGrainDto>> AddOrUpdateAsync(
        string userAddress,
        double totalPoint,
        long timestamp, 
        int activityId)
    {
        if (State.Id == Guid.Empty)
        {
            State.Id = Guid.NewGuid();
        }

        State.Timestamp = timestamp;
        State.ActivityId = activityId;
        // todo State.NumOfJoin
        
        var rankingItem = State.RankingList.Find(item => item.Address == userAddress);

        if (rankingItem != null)
        {
            rankingItem.TotalPoint = totalPoint;
        }
        else
        {
            State.RankingList.Add(new RankingInfo()
            {
                Address = userAddress,
                TotalPoint = totalPoint
            });
        }
        
        await WriteStateAsync();
        return new GrainResultDto<ActivityRankingSnapshotGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<ActivityRankingSnapshotState, ActivityRankingSnapshotGrainDto>(State)
        };
    }
    
}