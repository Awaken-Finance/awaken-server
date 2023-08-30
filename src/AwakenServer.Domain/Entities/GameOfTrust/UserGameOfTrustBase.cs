using System;
using Nest;

namespace AwakenServer.Entities.GameOfTrust
{
    public class UserGameOfTrustBase : MultiChainEntity<Guid>
    {
        [Keyword] public string Address { get; set; }
        public string ValueLocked { get; set; }
        public string ReceivedAmount { get; set; }
        public string ReceivedFineAmount { get; set; }
    }
}