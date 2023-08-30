using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.Trade
{
    public interface IUserTradeSummaryRepository : IRepository<UserTradeSummary, Guid>
    {
        Task<long> GetCountAsync(string chainId, Guid tradePairId, DateTime? minDateTime = null, DateTime? maxDateTime = null);
    }
}