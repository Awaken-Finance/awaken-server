using System;
using System.ComponentModel.DataAnnotations;

namespace AwakenServer.Trade.Dtos
{
    public class TradePairCreateDto
    {
        public Guid Id { get; set; }
        public string ChainId { get; set; }
        public string ChainName { get; set; }
        public Guid Token0Id { get; set; }
        public Guid Token1Id { get; set; }
        [Required]
        public string Address { get; set; }
        public double FeeRate { get; set; }
        public bool IsTokenReversed { get; set; }
    }
}