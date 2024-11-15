using AElf.OpenTelemetry;
using AwakenServer.AetherLinkApi;
using AwakenServer.CoinGeckoApi;
using AwakenServer.Grains;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AwakenServer.Silo;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AwakenServerGrainsModule),
    typeof(AwakenServerAetherLinkApiModule)
)]
public class AwakenServerServerOrleansSiloModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        bool isTelemetryEnabled = configuration.GetValue<bool>("OpenTelemetry:Enabled");

        if (isTelemetryEnabled)
        {
            context.Services.AddAssemblyOf<OpenTelemetryModule>();
        }
    }
    
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHostedService<AwakenServerHostedService>();
        var configuration = context.Services.GetConfiguration();
    }
}