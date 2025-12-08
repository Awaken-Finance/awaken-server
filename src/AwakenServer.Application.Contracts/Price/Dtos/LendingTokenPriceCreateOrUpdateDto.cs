using System;

namespace AwakenServer.Price.Dtos
{
    public class LendingTokenPriceCreateOrUpdateDto
    {
        public string ChainId { get; set; }
        public Guid TokenId { get; set; }
        public string Price { get; set; }
        public double PriceValue { get; set; }
        public long Timestamp { get; set; }
        public long BlockNumber { get; set; }
    }
}