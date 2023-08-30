using System;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.Trade
{
    public interface ITradePairRepository : IRepository<TradePair, Guid>
    {
        
    }
}