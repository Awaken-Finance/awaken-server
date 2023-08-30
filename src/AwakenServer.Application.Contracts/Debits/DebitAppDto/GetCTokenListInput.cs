using System;

namespace AwakenServer.Debits.DebitAppDto
{
    public class GetCTokenListInput
    {
        public string? ChainId { get; set; }
        public Guid? CompControllerId { get; set; }
        
        public Guid? CTokenId { get; set; }
        public string User { get; set; }
        public bool IsWithApy { get; set; }
        public bool IsWithUnderlyingTokenPrice { get; set; }
    }
}