using AwakenServer.Monitor;
using Volo.Abp.Modularity;
using Volo.Abp.ObjectExtending;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace AwakenServer
{
    [DependsOn(
        typeof(AwakenServerDomainSharedModule),
        typeof(AbpSettingManagementApplicationContractsModule),
        typeof(AbpTenantManagementApplicationContractsModule),
        typeof(AbpObjectExtendingModule),
        typeof(AwakenServerMonitorModule)
    )]
    public class AwakenServerApplicationContractsModule : AbpModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            AwakenServerDtoExtensions.Configure();
        }
    }
}
