using AwakenServer.EntityFrameworkCore;
using Volo.Abp.Modularity;

namespace AwakenServer.EntityHandler
{
    [DependsOn(
        typeof(AwakenServerApplicationModule),
        typeof(AwakenServerEntityFrameworkCoreModule),
        typeof(AwakenServerApplicationContractsModule)
    )]
    public class AwakenServerEntityHandlerCoreModule: AbpModule
    {
    }
}