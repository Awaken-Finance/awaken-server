using System;
using AwakenServer.Entities;
using Nest;

namespace AwakenServer.Farms.Entities
{
    public class FarmRecordBase : AwakenEntity<Guid>
    {
        public string TransactionHash { get; set; }
        [Keyword] public string User { get; set; }
        public string Amount { get; set; }
        public DateTime Date { get; set; }
        public BehaviorType BehaviorType { get; set; }
    }
}