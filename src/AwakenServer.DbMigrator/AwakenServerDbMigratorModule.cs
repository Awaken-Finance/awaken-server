using AwakenServer.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Modularity;

namespace AwakenServer.DbMigrator
{
    [DependsOn(
        typeof(AbpAutofacModule),
        typeof(AwakenServerEntityFrameworkCoreModule),
        typeof(AwakenServerApplicationContractsModule)
        )]
    public class AwakenServerDbMigratorModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpBackgroundJobOptions>(options => options.IsJobExecutionEnabled = false);
        }
    }
}
