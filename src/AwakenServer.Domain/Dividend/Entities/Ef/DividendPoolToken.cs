using System;

namespace AwakenServer.Dividend.Entities.Ef
{
    public class DividendPoolToken : DividendPoolTokenBase
    {
        public Guid PoolId { get; set; }
        public Guid DividendTokenId { get; set; }
    }
}