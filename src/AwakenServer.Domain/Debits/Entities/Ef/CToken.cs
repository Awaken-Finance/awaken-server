using System;

namespace AwakenServer.Debits.Entities.Ef
{
    public class CToken: EditableCTokenBase
    {
        public Guid UnderlyingTokenId { get; set; }
    }
}