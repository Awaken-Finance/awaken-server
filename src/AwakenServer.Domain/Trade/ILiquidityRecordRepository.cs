using System;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.Trade
{
    public interface ILiquidityRecordRepository : IRepository<LiquidityRecord, Guid>
    {
        
    }
}