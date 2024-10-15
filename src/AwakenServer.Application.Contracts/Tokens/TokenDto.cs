using System;
using Orleans;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Tokens
{
    [GenerateSerializer]
    public class TokenDto
    {
        [Id(0)]
        public Guid Id { get; set; }
        [Id(1)]
        public string Address { get; set; }
        [Id(2)]
        public string Symbol { get; set; }
        [Id(3)]
        public int Decimals { get; set; }
        [Id(4)]
        public string ImageUri { get; set; }
        [Id(5)]
        public string ChainId { get; set; }
    }
}