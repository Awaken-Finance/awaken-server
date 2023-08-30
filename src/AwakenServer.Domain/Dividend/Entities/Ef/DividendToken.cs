using System;

namespace AwakenServer.Dividend.Entities.Ef
{
    public class DividendToken : DividendTokenBase
    {
        public Guid DividendId { get; set; }
        public Guid TokenId { get; set; }
    }
}