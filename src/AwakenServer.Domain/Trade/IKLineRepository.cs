using System;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.Trade
{
    public interface IKLineRepository : IRepository<KLine, Guid>
    {
        
    }
}