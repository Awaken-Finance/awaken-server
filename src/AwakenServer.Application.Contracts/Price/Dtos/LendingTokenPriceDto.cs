using System;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Price.Dtos
{
    public class LendingTokenPriceDto : EntityDto<Guid>
    {
        public string ChainId { get; set; }
        public Guid TokenId { get; set; }
        public string Price { get; set; }
        public double PriceValue { get; set; }
        public long Timestamp { get; set; }
        public long BlockNumber { get; set; }
    }
}