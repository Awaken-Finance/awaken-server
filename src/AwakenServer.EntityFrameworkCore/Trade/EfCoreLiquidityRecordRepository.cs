// using System;
// using AwakenServer.EntityFrameworkCore;
// using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
// using Volo.Abp.EntityFrameworkCore;
//
// namespace AwakenServer.Trade
// {
//     public class EfCoreLiquidityRecordRepository : EfCoreRepository<AwakenServerDbContext, LiquidityRecord, Guid>,
//         ILiquidityRecordRepository
//     {
//         public EfCoreLiquidityRecordRepository(IDbContextProvider<AwakenServerDbContext> dbContextProvider)
//             : base(dbContextProvider)
//         {
//
//         }
//     }
// }