using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AwakenServer.Grains;

[DependsOn(typeof(AwakenServerDomainModule), typeof(AwakenServerApplicationContractsModule))]
public class AwakenServerGrainsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        base.ConfigureServices(context);
        Configure<AbpAutoMapperOptions>(options => options.AddMaps<AwakenServerGrainsModule>());
    }
}