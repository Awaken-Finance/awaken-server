using System;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Trade.Dtos
{
    public class TradePairDto : EntityDto<Guid>
    {
        public string Address { get; set; }
        public string ChainId { get; set; }
        public double FeeRate { get; set; }
        public bool IsTokenReversed { get; set; }
        public string Token0Symbol { get; set; }
        public string Token1Symbol { get; set; }
        public Guid Token0Id { get; set; }
        public Guid Token1Id { get; set; }
    }
}