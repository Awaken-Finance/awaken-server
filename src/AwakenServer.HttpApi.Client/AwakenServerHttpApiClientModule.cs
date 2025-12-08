using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using Volo.Abp.TenantManagement;
using Volo.Abp.SettingManagement;

namespace AwakenServer
{
    [DependsOn(
        typeof(AwakenServerApplicationContractsModule),
        typeof(AbpTenantManagementHttpApiClientModule),
        typeof(AbpSettingManagementHttpApiClientModule)
    )]
    public class AwakenServerHttpApiClientModule : AbpModule
    {
        public const string RemoteServiceName = "Default";

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddHttpClientProxies(
                typeof(AwakenServerApplicationContractsModule).Assembly,
                RemoteServiceName
            );
        }
    }
}
