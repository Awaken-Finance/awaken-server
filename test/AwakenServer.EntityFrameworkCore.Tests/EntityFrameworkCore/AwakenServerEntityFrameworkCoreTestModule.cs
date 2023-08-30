using Volo.Abp.Modularity;

namespace AwakenServer.EntityFrameworkCore
{
    [DependsOn(
        typeof(AwakenServerEntityFrameworkCoreModule),
        typeof(AwakenServerTestBaseModule),
        typeof(AwakenServerEntityFrameworkCoreModule)
        )]
    public class AwakenServerEntityFrameworkCoreTestModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            
        }
    }
}
