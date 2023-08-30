using System;

namespace AwakenServer.IDO.Dtos
{
    public class PublicOfferingRecordDto
    {
        public Guid Id { get; set; }
        public Guid ChannelId { get; set; }
        public string User { get; set; }
        public OperationType OperateType { get; set; }
        public long TokenAmount { get; set; }
        public long RaiseTokenAmount { get; set; }
        public long DateTime { get; set; }
        public string TransactionHash { get; set; }
        public string Channel { get; set; }
        public PublicOfferingWithTokenDto PublicOfferingInfo { get; set; }
    }
}