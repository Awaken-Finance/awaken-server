using System;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.Price
{
    public interface ILendingTokenPriceRepository : IRepository<LendingTokenPrice, Guid>
    {
        
    }
}