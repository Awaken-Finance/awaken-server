using System;
using AwakenServer.Tokens;

namespace AwakenServer.Dividend.DividendAppDto
{
    public class DividendUserRecordDto
    {
        public Guid Id { get; set; }
        public string TransactionHash { get; set; }
        public string User { get; set; }
        public long Date { get; set; }
        public string Amount { get; set; }
        public BehaviorType BehaviorType { get; set; }
        public DividendPoolBaseInfoDto PoolBaseInfo { get; set; }
        public TokenDto DividendToken { get; set; }
    }
}