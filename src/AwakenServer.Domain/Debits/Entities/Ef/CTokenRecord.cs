using System;

namespace AwakenServer.Debits.Entities.Ef
{
    public class CTokenRecord: CTokenRecordBase
    {
        public Guid CTokenId { get; set; }
        public Guid CompControllerId { get; set; }
        public Guid UnderlyingAssetTokenId { get; set; }
    }
}