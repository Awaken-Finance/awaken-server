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
//     public class EfCoreUserTradeSummaryRepository : EfCoreRepository<AwakenServerDbContext, UserTradeSummary, Guid>,
//         IUserTradeSummaryRepository
//     {
//         public EfCoreUserTradeSummaryRepository(IDbContextProvider<AwakenServerDbContext> dbContextProvider)
//             : base(dbContextProvider)
//         {
//
//         }
//         
//         public async Task<long> GetCountAsync(string chainId, Guid tradePairId, DateTime? minDateTime = null, DateTime? maxDateTime = null)
//         {
//             var query = await GetQueryableAsync();
//             query = query.Where(q => q.ChainId == chainId)
//                 .Where(q => q.TradePairId == tradePairId)
//                 .WhereIf(minDateTime.HasValue, q => q.LatestTradeTime >= minDateTime)
//                 .WhereIf(maxDateTime.HasValue, q => q.LatestTradeTime <= maxDateTime);
//             return await query.LongCountAsync();
//         }
//     }
// }