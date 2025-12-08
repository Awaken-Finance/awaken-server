// using System;
// using AwakenServer.EntityFrameworkCore;
// using Volo.Abp.EntityFrameworkCore;
//
// namespace AwakenServer.Tokens
// {
//     public class EfCoreTokenRepository : EfCoreCacheRepository<AwakenServerDbContext, Token, Guid>, ITokenRepository
//     {
//         public EfCoreTokenRepository(IDbContextProvider<AwakenServerDbContext> dbContextProvider)
//             : base(dbContextProvider)
//         {
//             
//         }
//         
//     }
// }