using AwakenServer.CoinGeckoApi;
using AwakenServer.Grains;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AwakenServer.Silo;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AwakenServerGrainsModule),
    typeof(AwakenServerCoinGeckoApiModule)
)]
public class AwakenServerServerOrleansSiloModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHostedService<AwakenServerHostedService>();
    }
}