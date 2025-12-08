using System;
using System.ComponentModel.DataAnnotations;

namespace AwakenServer.Favorite
{
    public class FavoriteCreateDto
    {
        [Required]
        public Guid TradePairId { get; set; }
        [Required]
        public string Address { get; set; }
        public long Timestamp { get; set; }
    }
}