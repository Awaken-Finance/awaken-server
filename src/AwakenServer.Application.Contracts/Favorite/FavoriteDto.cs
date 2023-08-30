using System;

namespace AwakenServer.Favorite
{
    public class FavoriteDto
    {
        public string Id { get; set; }
        public Guid TradePairId { get; set; }
        public string Address { get; set; }
        public long Timestamp { get; set; }
    }
}