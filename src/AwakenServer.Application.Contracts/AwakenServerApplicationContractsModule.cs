using Volo.Abp.Account;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.ObjectExtending;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace AwakenServer
{
    [DependsOn(
        typeof(AwakenServerDomainSharedModule),
        //typeof(AbpAccountApplicationContractsModule),
        //typeof(AbpFeatureManagementApplicationContractsModule),
        //typeof(AbpIdentityApplicationContractsModule),
        //typeof(AbpPermissionManagementApplicationContractsModule),
        typeof(AbpSettingManagementApplicationContractsModule),
        typeof(AbpTenantManagementApplicationContractsModule),
        typeof(AbpObjectExtendingModule)
    )]
    public class AwakenServerApplicationContractsModule : AbpModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            AwakenServerDtoExtensions.Configure();
        }
    }
}
