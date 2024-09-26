using System.Collections.Generic;

namespace AwakenServer.Activity;

public class RankingListSnapshot : ActivityBase
{
    public long Timestamp { get; set; }
    public long NumOfJoin { get; set; } // JoinRecord Count union UserActivityInfo count
    public List<RankingInfo> RankingList { get; set; }
}

public class RankingInfo
{
    public string Address { get; set; }
    public long TotalPoint { get; set; }
}