using System;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Trade.Dtos
{
    public class TradeRecordIndexDto : EntityDto<Guid>
    {
        public string ChainId { get; set; }
        public TradePairWithTokenDto TradePair { get; set; }
        public string Address { get; set; }
        public TradeSide Side { get; set; }
        public double Price { get; set; }
        public double TotalPriceInUsd { get; set; }
        public double TotalFee { get; set; }
        public double TransactionFee { get; set; }
        public string Token0Amount { get; set; }
        public string Token1Amount { get; set; }
        public long Timestamp { get; set; }
        public string TransactionHash { get; set; }
        public string Channel { get; set; }
        public string Sender { get; set; }
    }
}