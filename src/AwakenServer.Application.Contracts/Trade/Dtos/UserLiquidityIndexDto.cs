using System;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Trade.Dtos
{
    public class UserLiquidityIndexDto: EntityDto<Guid>
    {
        public TradePairWithTokenDto TradePair { get; set; }
        public string Address { get; set; }
        public string LpTokenAmount { get; set; }
        public double AssetUSD { get; set; }
        public string Token0Amount { get; set; }
        public string Token1Amount { get; set; }
    }
}