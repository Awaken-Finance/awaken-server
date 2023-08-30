using System;
using AwakenServer.Entities;
using Nest;

namespace AwakenServer.IDO.Entities
{
    public abstract class UserPublicOfferingBase : MultiChainEntity<Guid>
    {
        [Keyword] public string User { get; set; }
        public long TokenAmount { get; set; }
        public long RaiseTokenAmount { get; set; }
        public bool IsHarvest { get; set; }
    }
}