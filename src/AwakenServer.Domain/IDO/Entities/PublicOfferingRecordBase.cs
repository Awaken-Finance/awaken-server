using System;
using AwakenServer.Entities;
using Nest;

namespace AwakenServer.IDO.Entities
{
    public abstract class PublicOfferingRecordBase : MultiChainEntity<Guid>
    {
        [Keyword] public string User { get; set; }
        public OperationType OperateType { get; set; }
        public long TokenAmount { get; set; }
        public long RaiseTokenAmount { get; set; }
        public DateTime DateTime { get; set; }
        public string TransactionHash { get; set; }
        [Keyword] public string Channel { get; set; }
    }
}