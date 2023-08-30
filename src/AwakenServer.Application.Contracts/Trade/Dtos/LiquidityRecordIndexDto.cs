using System;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Trade.Dtos
{
    public class LiquidityRecordIndexDto : EntityDto<Guid>
    {
        public string ChainId { get; set; }
        public TradePairWithTokenDto TradePair { get; set; }
        public string Address { get; set; }
        public string Token0Amount { get; set; }
        public string Token1Amount { get; set; }
        public string LpTokenAmount { get; set; }
        public long Timestamp { get; set; }
        public LiquidityType Type { get; set; }
        public string TransactionHash { get; set; }
        public double TransactionFee { get; set; }
        public string Channel { get; set; }
        public string Sender { get; set; }
    }
}