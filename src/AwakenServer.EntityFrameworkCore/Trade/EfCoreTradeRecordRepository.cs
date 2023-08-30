// using System;
// using AwakenServer.EntityFrameworkCore;
// using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
// using Volo.Abp.EntityFrameworkCore;
//
// namespace AwakenServer.Trade
// {
//     public class EfCoreTradeRecordRepository : EfCoreRepository<AwakenServerDbContext, TradeRecord, Guid>,
//         ITradeRecordRepository
//     {
//         public EfCoreTradeRecordRepository(IDbContextProvider<AwakenServerDbContext> dbContextProvider)
//             : base(dbContextProvider)
//         {
//
//         }
//     }
// }