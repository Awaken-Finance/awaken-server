namespace AwakenServer.Grains.Grain.Favorite;

public class FavoriteGrainDto
{
    public string Id { get; set; }
    public Guid TradePairId { get; set; }
    public string Address { get; set; }
    public long Timestamp { get; set; }
}