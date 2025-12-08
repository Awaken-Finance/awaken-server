using AwakenServer.ContractEventHandler;
using AwakenServer.EntityHandler;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp.Modularity;

namespace AwakenServer
{
    [DependsOn(
        typeof(AwakenServerContractEventHandlerCoreModule),
        typeof(AwakenServerDomainTestModule),
        typeof(AwakenServerEntityHandlerCoreModule)
        )]
    public class AwakenServerContractEventHandlerCoreTestModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IClusterClient>(sp => sp.GetService<ClusterFixture>().Cluster.Client);
        }
    }
}