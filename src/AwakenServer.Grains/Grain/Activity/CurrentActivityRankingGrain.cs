using AwakenServer.Grains.State.Activity;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Grains.Grain.Activity;

public class CurrentActivityRankingGrain : Grain<CurrentActivityRankingState>, ICurrentActivityRankingGrain
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
        int activityId,
        bool isNewUser)
    {
        if (State.Id == Guid.Empty)
        {
            State.Id = Guid.NewGuid();
        }

        State.Timestamp = timestamp;
        State.ActivityId = activityId;
        if (isNewUser)
        {
            State.NumOfJoin++;
        }

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
        State.RankingList.Sort((r1, r2) => r2.TotalPoint.CompareTo(r1.TotalPoint));
        if (State.RankingList.Count > 1000)
        {
            State.RankingList.RemoveRange(1000, State.RankingList.Count - 1000);
        }

        await WriteStateAsync();
        return new GrainResultDto<ActivityRankingSnapshotGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<CurrentActivityRankingState, ActivityRankingSnapshotGrainDto>(State)
        };
    }

    public async Task<GrainResultDto<ActivityRankingSnapshotGrainDto>> AddNumOfPointAsync(int activityId, int num)
    {
        if (State.Id == Guid.Empty)
        {
            State.Id = Guid.NewGuid();
            State.ActivityId = activityId;
        }
        State.NumOfJoin += num;
        await WriteStateAsync();
        return new GrainResultDto<ActivityRankingSnapshotGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<CurrentActivityRankingState, ActivityRankingSnapshotGrainDto>(State)
        };
    }
}