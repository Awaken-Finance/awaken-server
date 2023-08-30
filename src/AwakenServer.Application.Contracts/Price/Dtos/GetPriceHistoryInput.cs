using System;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Price.Dtos
{
    public class GetPriceHistoryInput : PagedAndSortedResultRequestDto
    {
        public string ChainId { get; set; }
        public Guid TokenId { get; set; }
        public long TimestampMin { get; set; }
        public long TimestampMax { get; set; }
    }
}