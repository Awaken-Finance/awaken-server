// using System.IO;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore.Design;
// using Microsoft.Extensions.Configuration;
//
// namespace AwakenServer.EntityFrameworkCore
// {
//     /* This class is needed for EF Core console commands
//      * (like Add-Migration and Update-Database commands) */
//     public class AwakenServerDbContextFactory : IDesignTimeDbContextFactory<AwakenServerDbContext>
//     {
//         public AwakenServerDbContext CreateDbContext(string[] args)
//         {
//             AwakenServerEfCoreEntityExtensionMappings.Configure();
//
//             var configuration = BuildConfiguration();
//
//             var builder = new DbContextOptionsBuilder<AwakenServerDbContext>()
//                 .UseMySql(configuration.GetConnectionString("Default"), MySqlServerVersion.LatestSupportedServerVersion);
//
//             return new AwakenServerDbContext(builder.Options);
//         }
//
//         private static IConfigurationRoot BuildConfiguration()
//         {
//             var builder = new ConfigurationBuilder()
//                 .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../AwakenServer.DbMigrator/"))
//                 .AddJsonFile("appsettings.json", optional: false);
//
//             return builder.Build();
//         }
//     }
// }
