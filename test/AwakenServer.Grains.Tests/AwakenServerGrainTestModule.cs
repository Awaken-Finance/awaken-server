using AwakenServer.CoinGeckoApi;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp.AutoMapper;
using Volo.Abp.Caching;
using Volo.Abp.Modularity;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Grains.Tests;

[DependsOn(
    typeof(AwakenServerGrainsModule),
    typeof(AwakenServerDomainTestModule),
    typeof(AwakenServerDomainModule),
    typeof(AwakenServerCoinGeckoApiModule),
    typeof(AbpCachingModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpObjectMappingModule)
)]
public class AwakenServerGrainTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IClusterClient>(sp => sp.GetService<ClusterFixture>().Cluster.Client);
        context.Services.Configure<CoinGeckoOptions>(o => { o.CoinIdMapping = new Dictionary<string, string>
        {
            { "ELF", "aelf" }
        }; });
    }
}