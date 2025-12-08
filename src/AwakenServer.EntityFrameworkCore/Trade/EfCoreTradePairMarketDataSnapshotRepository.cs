// using System;
// using System.Linq;
// using System.Threading.Tasks;
// using AwakenServer.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore;
// using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
// using Volo.Abp.EntityFrameworkCore;
//
// namespace AwakenServer.Trade
// {
//     public class EfCoreTradePairMarketDataSnapshotRepository : EfCoreRepository<AwakenServerDbContext, TradePairMarketDataSnapshot, Guid>,
//         ITradePairMarketDataSnapshotRepository
//     {
//         public EfCoreTradePairMarketDataSnapshotRepository(IDbContextProvider<AwakenServerDbContext> dbContextProvider)
//             : base(dbContextProvider)
//         {
//             
//         }
//
//         public async Task<TradePairMarketDataSnapshot> GetLastOrDefaultAsync(string chainId, Guid tradePairId, DateTime? maxDateTime = null)
//         {
//             var query = await GetQueryableAsync();
//             query = query.Where(q => q.ChainId == chainId).Where(q => q.TradePairId == tradePairId)
//                 .WhereIf(maxDateTime.HasValue, q => q.Timestamp <= maxDateTime).OrderBy(q=>q.Timestamp);
//             return await query.LastOrDefaultAsync();
//         }
//     }
// }