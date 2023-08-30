using AElf.Client.Service;
using AwakenServer.Chains;
using AwakenServer.CMS;
using AwakenServer.ContractEventHandler.Application;
using AwakenServer.Debits.Options;
using AwakenServer.Dividend.Options;
using AwakenServer.Farms.Helpers;
using AwakenServer.Farms.Options;
using AwakenServer.Grains;
using AwakenServer.Trade;
using AwakenServer.Web3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.AutoMapper;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace AwakenServer
{
    [DependsOn(
        typeof(AwakenServerDomainModule),
        typeof(AwakenServerApplicationContractsModule),
        typeof(AwakenServerGrainsModule),
        typeof(AbpTenantManagementApplicationModule),
        typeof(AbpSettingManagementApplicationModule)
        )]
    public class AwakenServerApplicationModule : AbpModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            // PreConfigure<AbpEventBusOptions>(options =>
            // {
            //     options.EnabledErrorHandle = true;
            //     options.UseRetryStrategy(retryStrategyOptions =>
            //     {
            //         retryStrategyOptions.IntervalMillisecond = 1000;
            //         retryStrategyOptions.MaxRetryAttempts = int.MaxValue;
            //     });
            // });
        }
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpAutoMapperOptions>(options =>
            {
                options.AddMaps<AwakenServerApplicationModule>();
            });
            
            var configuration = context.Services.GetConfiguration();
            Configure<StableCoinOptions>(configuration.GetSection("StableCoin"));
            Configure<MainCoinOptions>(configuration.GetSection("MainCoin"));
            Configure<KLinePeriodOptions>(configuration.GetSection("KLinePeriods"));
            Configure<AssetShowOptions>(configuration.GetSection("AssetShow"));
            Configure<ApiOptions>(configuration.GetSection("Api"));
            Configure<FarmOption>(configuration.GetSection("Farm"));
            Configure<FarmTokenOptions>(configuration.GetSection("FarmToken"));
            Configure<ISTARTokenOptions>(configuration.GetSection("ISTARToken"));
            Configure<DebitOption>(configuration.GetSection("Debit"));
            Configure<InterestModelOption>(configuration.GetSection("InterestModel"));
            Configure<DividendOption>(configuration.GetSection("Dividend"));
            Configure<GraphQLOptions>(configuration.GetSection("GraphQL"));
            Configure<CmsOptions>(configuration.GetSection("Cms"));


            context.Services.AddTransient<IBlockchainClientProvider, AElfClientProvider>();
            context.Services.AddTransient<IAElfClientProvider, AElfClientProvider>();
            context.Services.AddTransient<IBlockchainClientProvider, Web3Provider>();
            context.Services.AddSingleton<IBlockchainClientFactory<AElfClient>, AElfClientFactory>();
            context.Services.AddSingleton<IBlockchainClientFactory<Nethereum.Web3.Web3>, Web3ClientFactory>();
            context.Services.AddSingleton<IHttpService>(provider =>
            {
                return new HttpService(3);
            });
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var farmOptions = context.ServiceProvider.GetService<IOptionsSnapshot<FarmOption>>();
            if (farmOptions == null)
            {
                return;
            }

            ProjectTokenCalculationHelper.LastTerm = farmOptions.Value.LastTerm - 1;
        }
    }
}
