using System;
using AwakenServer.Tokens;
using Orleans;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Trade.Dtos
{
    [GenerateSerializer]
    public class TradePairWithTokenDto
    {
        [Id(0)]
        public Guid Id { get; set; }
        [Id(1)]
        public string ChainId { get; set; }
        [Id(2)]
        public string Address { get; set; }
        [Id(3)]
        public double FeeRate { get; set; }
        [Id(4)]
        public bool IsTokenReversed { get; set; }
        [Id(5)]
        public TokenDto Token0 { get; set; }
        [Id(6)]
        public TokenDto Token1 { get; set; }
    }
}