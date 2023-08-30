using System;

namespace AwakenServer.IDO.Entities.Ef
{
    public class PublicOffering : PublicOfferingBase
    {
        public Guid TokenId { get; set; }
        public Guid RaiseTokenId { get; set; }
        public long CurrentAmount { get; set; }
        public long RaiseCurrentAmount { get; set; }
    }
}