using System;
using AwakenServer.Entities;
using Nest;

namespace AwakenServer.IDO.Entities
{
    public abstract class PublicOfferingBase : MultiChainEntity<Guid>
    {
        public long OrderRank { get; set; }
        public string TokenContractAddress { get; set; }
        public long MaxAmount { get; set; }
        public long RaiseMaxAmount { get; set; }
        [Keyword] public string Publisher { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class PublicOfferingWithToken : PublicOfferingBase
    {
        public Tokens.Token Token { get; set; }
        public Tokens.Token RaiseToken { get; set; }
    }
}