using System;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.Trade
{
    public interface ITradeRecordRepository : IRepository<TradeRecord, Guid>
    {
        
    }
}