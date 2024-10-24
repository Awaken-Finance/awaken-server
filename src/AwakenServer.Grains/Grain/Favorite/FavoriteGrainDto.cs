using System;

namespace AwakenServer.Grains.Grain.Favorite;

[GenerateSerializer]
public class FavoriteGrainDto
{
    [Id(0)] public string Id { get; set; }
    [Id(1)] public Guid TradePairId { get; set; }
    [Id(2)] public string Address { get; set; }
    [Id(3)] public long Timestamp { get; set; }
}