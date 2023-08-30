using System;
using AwakenServer.Entities;
using Nest;

namespace AwakenServer.Debits.Entities
{
    public class CTokenRecordBase : AwakenEntity<Guid>
    {
        [Keyword] public string TransactionHash { get; set; }
        [Keyword] public string User { get; set; }
        public string CTokenAmount { get; set; }
        public string UnderlyingTokenAmount { get; set; }
        public DateTime Date { get; set; }
        public BehaviorType BehaviorType { get; set; }
        [Keyword] public string Channel { get; set; }
    }
}