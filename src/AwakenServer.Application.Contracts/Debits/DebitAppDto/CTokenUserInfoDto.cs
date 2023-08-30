using System;

namespace AwakenServer.Debits.DebitAppDto
{
    public class CTokenUserInfoDto
    {
        public Guid Id { get; set; }
        public string User { get; set; }
        public string AccumulativeBorrowComp { get; set; }
        public string AccumulativeSupplyComp { get; set; }
        public string TotalBorrowAmount { get; set; }
        public bool IsEnteredMarket { get; set; }
        public CTokenBaseDto CTokenInfo { get; set; }
        public CompControllerBaseDto CompInfo { get; set;}
        public DebitTokenDto UnderlyingToken { get; set; }
    }
}