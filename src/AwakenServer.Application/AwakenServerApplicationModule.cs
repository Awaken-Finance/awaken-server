using AElf.Client.Service;
using AwakenServer.Asset;
using AwakenServer.Chains;
using AwakenServer.CMS;
using AwakenServer.ContractEventHandler.Application;
using AwakenServer.Grains;
using AwakenServer.Price;
using AwakenServer.Provider;
using AwakenServer.Trade;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.AutoMapper;
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
            Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AwakenServerApplicationModule>(); });

            var configuration = context.Services.GetConfiguration();
            Configure<StableCoinOptions>(configuration.GetSection("StableCoin"));
            Configure<MainCoinOptions>(configuration.GetSection("MainCoin"));
            Configure<KLinePeriodOptions>(configuration.GetSection("KLinePeriods"));
            Configure<AssetShowOptions>(configuration.GetSection("AssetShow"));
            Configure<ApiOptions>(configuration.GetSection("Api"));
            Configure<GraphQLOptions>(configuration.GetSection("GraphQL"));
            Configure<CmsOptions>(configuration.GetSection("Cms"));
            Configure<AssetWhenNoTransactionOptions>(configuration.GetSection("AssetWhenNoTransaction"));
            Configure<ContractsTokenOptions>(configuration.GetSection("ContractsTokenOptions"));
            Configure<TokenPriceOptions>(configuration.GetSection("TokenPriceOptions"));
            Configure<PortfolioOptions>(configuration.GetSection("PortfolioOptions"));

            context.Services.AddTransient<IBlockchainClientProvider, AElfClientProvider>();
            context.Services.AddTransient<IAElfClientProvider, AElfClientProvider>();
            context.Services.AddSingleton<IBlockchainClientFactory<AElfClient>, AElfClientFactory>();
            context.Services.AddSingleton<IHttpService>(provider => { return new HttpService(3); });
            context.Services.AddSingleton<ITradePairMarketDataProvider, TradePairMarketDataProvider>();
            context.Services.AddSingleton<IRevertProvider, RevertProvider>();
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
        }
    }
}