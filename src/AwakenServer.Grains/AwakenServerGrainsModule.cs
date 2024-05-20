using Aetherlink.PriceServer;
using Aetherlink.PriceServer.Common;
using Aetherlink.PriceServer.Options;
using AwakenServer.Grains.Grain.Tokens.TokenPrice;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AwakenServer.Grains;

[DependsOn(typeof(AwakenServerDomainModule), 
    typeof(AwakenServerApplicationContractsModule),
    typeof(AetherlinkPriceServerModule))]
public class AwakenServerGrainsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        base.ConfigureServices(context);
        Configure<AbpAutoMapperOptions>(options => options.AddMaps<AwakenServerGrainsModule>());
        
        context.Services.AddSingleton<ITokenPriceProvider, TokenPriceAetherlinkProvider>();
    }
}