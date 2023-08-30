namespace AwakenServer.Grains.State.Favorite;

public class FavoriteState
{
    public string Id { get; set; }
    public List<FavoriteInfo> FavoriteInfos { get; set; } = new();
}

public class FavoriteInfo
{
    public string Id { get; set; }
    public string TradePairId { get; set; }
    public string Address { get; set; }
    public long Timestamp { get; set; }
}