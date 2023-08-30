using System;

namespace AwakenServer.Dividend.Entities.Ef
{
    public class DividendUserRecord : DividendUserRecordBase
    {
        public Guid PoolId { get; set; }
        public Guid DividendTokenId { get; set; }
    }
}