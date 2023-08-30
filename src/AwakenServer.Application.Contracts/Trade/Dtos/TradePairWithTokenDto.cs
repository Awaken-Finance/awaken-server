using System;
using AwakenServer.Tokens;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Trade.Dtos
{
    public class TradePairWithTokenDto: EntityDto<Guid>
    {
        public string ChainId { get; set; }
        public string Address { get; set; }
        public double FeeRate { get; set; }
        public bool IsTokenReversed { get; set; }
        public TokenDto Token0 { get; set; }
        public TokenDto Token1 { get; set; }
    }
}