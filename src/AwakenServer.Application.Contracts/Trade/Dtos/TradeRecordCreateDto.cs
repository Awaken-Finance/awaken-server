using System;
using System.ComponentModel.DataAnnotations;

namespace AwakenServer.Trade.Dtos
{
    public class TradeRecordCreateDto
    {
        public string ChainId { get; set; }
        public Guid TradePairId { get; set; }
        [Required] public string Address { get; set; }
        public TradeSide Side { get; set; } = ((TradeSide[])Enum.GetValues(typeof(TradeSide)))[0];
        public string Token0Amount { get; set; }
        public string Token1Amount { get; set; }
        public double TotalFee { get; set; }
        public long Timestamp { get; set; }
        [Required] public string TransactionHash { get; set; }
        public string Channel { get; set; }
        public string Sender { get; set; }
        public long BlockHeight { get; set; }

        public bool IsRevert { get; set; }
        public string MethodName { get; set; }
        public bool IsLimitOrder { get; set; }
    }
}