using AwakenServer.Chains;
using AwakenServer.EntityHandler;
using AwakenServer.Grains.Tests;
using AwakenServer.Provider;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AwakenServer;

[DependsOn(
    typeof(AwakenServerApplicationModule),
    typeof(AwakenServerGrainTestModule),
    typeof(AwakenServerDomainTestModule),
    typeof(AwakenServerEntityHandlerCoreModule)
)]
public class AwakenServerApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<ChainTestHelper>();
        context.Services.AddSingleton<TradePairTestHelper>();
        context.Services.AddSingleton(sp => sp.GetService<ClusterFixture>().Cluster.Client);
        context.Services.AddSingleton<IAElfClientProvider, MockAelfClientProvider>();
        context.Services.AddMassTransitTestHarness(cfg => { });
    }
}