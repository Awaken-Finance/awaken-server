using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Trade.Dtos
{
    public class GetLiquidityRecordsInput : PagedAndSortedResultRequestDto
    {
        [Required]
        public string ChainId { get; set; }
        public Guid? TradePairId { get; set; }
        public string TransactionHash { get; set; }
        [Required]
        public string Address { get; set; }
        public string TokenSymbol { get; set; }
        public long TimestampMin { get; set; }
        public long TimestampMax { get; set; }
        public double FeeRate { get; set; }
        public LiquidityType? Type { get; set; }

        public GetLiquidityRecordsInput()
        {

        }
    }
}