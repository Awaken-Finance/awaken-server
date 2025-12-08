// using System;
// using AwakenServer.EntityFrameworkCore;
// using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
// using Volo.Abp.EntityFrameworkCore;
//
// namespace AwakenServer.Trade
// {
//     public class EfCoreKLineRepository : EfCoreRepository<AwakenServerDbContext, KLine, Guid>,
//         IKLineRepository
//     {
//         public EfCoreKLineRepository(IDbContextProvider<AwakenServerDbContext> dbContextProvider)
//             : base(dbContextProvider)
//         {
//
//         }
//     }
// }