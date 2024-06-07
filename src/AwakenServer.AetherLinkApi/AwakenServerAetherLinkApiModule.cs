using System;
using Aetherlink.PriceServer;
using AwakenServer.Grains.Grain.Tokens.TokenPrice;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Caching;
using Volo.Abp.Modularity;

namespace AwakenServer.AetherLinkApi;

[DependsOn(
    typeof(AbpCachingModule),
    typeof(AetherlinkPriceServerModule))]

public class AwakenServerAetherLinkApiModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<AetherLinkOptions>(configuration.GetSection("AetherLinkApi"));
        context.Services.AddSingleton<ITokenPriceProvider, AetherLinkTokenPriceProvider>();
    }
}