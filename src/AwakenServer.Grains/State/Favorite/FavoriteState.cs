using System.Collections.Generic;

namespace AwakenServer.Grains.State.Favorite;

[GenerateSerializer]
public class FavoriteState
{
    [Id(0)]
    public string Id { get; set; }
    [Id(1)]
    public List<FavoriteInfo> FavoriteInfos { get; set; } = new();
}

[GenerateSerializer]
public class FavoriteInfo
{
    [Id(0)]
    public string Id { get; set; }
    [Id(1)]
    public string TradePairId { get; set; }
    [Id(2)]
    public string Address { get; set; }
    [Id(3)]
    public long Timestamp { get; set; }
}