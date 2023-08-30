using System;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.GameOfTrust.DTos
{
    public class GetMarketDataInput:PagedAndSortedResultRequestDto
    {
        public string? ChainId { get; set; }
        public Guid? Id { get; set; }
        public long TimestampMin { get; set; }
        public long TimestampMax { get; set; }
    }
}