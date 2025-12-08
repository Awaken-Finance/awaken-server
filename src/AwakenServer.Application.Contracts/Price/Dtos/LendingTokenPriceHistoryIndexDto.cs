using System;
using AwakenServer.Tokens;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Price.Dtos
{
    public class LendingTokenPriceHistoryIndexDto : EntityDto<Guid>
    {
        public string ChainId { get; set; }
        public TokenDto Token { get; set; }
        public string Price { get; set; }
        public long Timestamp { get; set; }
    }
}