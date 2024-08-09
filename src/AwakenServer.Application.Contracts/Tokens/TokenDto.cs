using System;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Tokens
{
    public class TokenDto : EntityDto<Guid>
    {
        public string Address { get; set; }
        public string Symbol { get; set; }
        public int Decimals { get; set; }
        public string ChainId { get; set; }
        public string ImageUri { get; set; }
    }
}