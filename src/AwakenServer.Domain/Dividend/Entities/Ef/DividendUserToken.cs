using System;

namespace AwakenServer.Dividend.Entities.Ef
{
    public class DividendUserToken : DividendUserTokenBase
    {
        public Guid DividendTokenId { get; set; }
        public Guid PoolId { get; set; }
    }
}