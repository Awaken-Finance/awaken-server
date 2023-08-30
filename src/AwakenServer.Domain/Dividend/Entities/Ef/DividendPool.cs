using System;

namespace AwakenServer.Dividend.Entities.Ef
{
    public class DividendPool : EditableDividendPoolBase
    {
        public Guid DividendId { get; set; }
        public Guid PoolTokenId { get; set; }
    }
}