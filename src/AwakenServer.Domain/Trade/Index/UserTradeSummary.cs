using System;
using AElf.Indexing.Elasticsearch;

namespace AwakenServer.Trade.Index
{
    public class UserTradeSummary : AwakenServer.Trade.UserTradeSummary, IIndexBuild
    {
        public UserTradeSummary()
        {
        }

        public UserTradeSummary(Guid id)
            : base(id)
        {
        }
    }
}