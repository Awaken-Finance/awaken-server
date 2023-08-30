using System;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Debits.DebitAppDto
{
    public class CTokenRecordDto: EntityDto<Guid>
    {
        public string TransactionHash { get; set; }
        public string User { get; set; }
        public string CTokenAmount { get; set; }
        public double CTokenDecimalAmount { get; set; }
        public string UnderlyingTokenAmount { get; set; }
        public double UnderlyingTokenDecimalAmount { get; set; }
        public long Timestamp { get; set; }
        public BehaviorType BehaviorType { get; set; }
        public CTokenBaseDto CToken { get; set; }
        public CompControllerBaseDto CompControllerInfo { get; set; }
        public DebitTokenDto UnderlyingAssetToken { get; set; }
        public string Channel { get; set; }
    }
}