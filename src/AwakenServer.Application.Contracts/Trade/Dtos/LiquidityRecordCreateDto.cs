using System;
using System.ComponentModel.DataAnnotations;

namespace AwakenServer.Trade.Dtos
{
    public class LiquidityRecordCreateDto
    {
        public string ChainId { get; set; }
        public Guid TradePairId { get; set; }
        [Required]
        public string Address { get; set; }
        public string Token0Amount { get; set; }
        public string Token1Amount { get; set; }
        public string LpTokenAmount { get; set; }
        public long Timestamp { get; set; }
        public LiquidityType Type { get; set; } = ((LiquidityType[])Enum.GetValues(typeof(LiquidityType)))[0];
        [Required]
        public string TransactionHash { get; set; }
        public string Channel { get; set; }
        public string Sender { get; set; }
    }
}