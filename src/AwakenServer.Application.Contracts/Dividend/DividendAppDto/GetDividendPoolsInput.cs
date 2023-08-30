using System;

namespace AwakenServer.Dividend.DividendAppDto
{
    public class GetDividendPoolsInput
    {
        public Guid? DividendId { get; set; }
        public Guid? PoolId { get; set; }
    }
}