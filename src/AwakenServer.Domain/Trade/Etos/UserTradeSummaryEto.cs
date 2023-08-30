using System;
using AutoMapper;

namespace AwakenServer.Trade.Etos
{
    [AutoMap(typeof(UserTradeSummary))]
    public class UserTradeSummaryEto : UserTradeSummary
    {
        public UserTradeSummaryEto()
        {
        }

        public UserTradeSummaryEto(Guid id)
            : base(id)
        {
            Id = id;
        }
    }
}