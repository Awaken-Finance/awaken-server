using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.Trade
{
    public interface ITradePairMarketDataSnapshotRepository : IRepository<TradePairMarketDataSnapshot, Guid>
    {
        Task<TradePairMarketDataSnapshot> GetLastOrDefaultAsync(string chainId, Guid tradePairId,
            DateTime? maxDateTime = null);
    }
}