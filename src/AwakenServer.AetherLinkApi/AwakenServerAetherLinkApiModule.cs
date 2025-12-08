using System;
using Aetherlink.PriceServer;
using AwakenServer.Grains.Grain.Tokens.TokenPrice;
using AwakenServer.Price;
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
        context.Services.AddSingleton<ITokenPriceProvider, AetherLinkTokenPriceProvider>();
    }
}