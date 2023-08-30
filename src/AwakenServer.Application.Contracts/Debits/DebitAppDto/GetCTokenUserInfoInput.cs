using System;

namespace AwakenServer.Debits.DebitAppDto
{
    public class GetCTokenUserInfoInput
    {
        public string? ChainId { get; set; }
        public Guid? CompControllerId { get; set; }
        
        public Guid? CTokenId { get; set; }
        public string User { get; set; }
    }
}