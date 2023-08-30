using System;
using AwakenServer.GameOfTrust;
using Nest;

namespace AwakenServer.Entities.GameOfTrust
{
    public abstract class GameOfTrustRecordBase: MultiChainEntity<Guid>
    {   
        [Keyword]
        public string Address { get; set; }
        public BehaviorType Type { get; set; }
        public string Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public string TransactionHash { get; set; }
    }
}